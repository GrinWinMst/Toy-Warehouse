#!/usr/bin/env bash
# =============================================================================
# restore.sh — Скрипт восстановления системы Toy-Warehouse «из нуля»
#
# Использование:
#   ./scripts/restore.sh <путь_к_дампу.sql.gz>
#
# Пример:
#   ./scripts/restore.sh backups/db/warehouse_db_2026-06-14_02-00-00.sql.gz
#
# ВНИМАНИЕ: Скрипт ПЕРЕСОЗДАЁТ базу данных!
#   Все существующие данные в БД будут удалены и заменены данными из дампа.
#
# Требования:
#   - psql, createdb, dropdb (из пакета postgresql-client)
#   - Переменные окружения из .env
# =============================================================================

set -euo pipefail

# ─── Цвета для вывода ─────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log() {
    local level="$1"
    local message="$2"
    local timestamp
    timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo -e "[${timestamp}] [${level}] ${message}"
}

log_info()  { log "${BLUE}INFO${NC} " "$1"; }
log_ok()    { log "${GREEN}OK${NC}   " "$1"; }
log_warn()  { log "${YELLOW}WARN${NC} " "$1"; }
log_error() { log "${RED}ERROR${NC}" "$1"; }

# ─── Проверка аргументов ──────────────────────────────────────────────────────
if [[ $# -lt 1 ]]; then
    log_error "Не указан путь к файлу дампа."
    echo ""
    echo "  Использование: $0 <путь_к_дампу.sql.gz>"
    echo "  Пример:        $0 backups/db/warehouse_db_2026-06-14_02-00-00.sql.gz"
    echo ""
    echo "  Доступные дампы:"
    find "$(dirname "$0")/../backups/db" -name "*.sql.gz" 2>/dev/null \
        | sort -r | head -10 | sed 's/^/    /'
    exit 1
fi

DUMP_FILE="$1"

# ─── Проверка существования файла дампа ──────────────────────────────────────
if [[ ! -f "$DUMP_FILE" ]]; then
    log_error "Файл дампа не найден: ${DUMP_FILE}"
    exit 1
fi

# ─── Директории ───────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# ─── Загрузка переменных окружения ────────────────────────────────────────────
ENV_FILE="${PROJECT_DIR}/WarehouseAPI/.env"
if [[ -f "$ENV_FILE" ]]; then
    log_info "Загрузка конфигурации из ${ENV_FILE}"
    set -a
    # shellcheck disable=SC1090
    source "$ENV_FILE"
    set +a
else
    log_warn "Файл .env не найден. Используются переменные среды."
fi

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-warehouse_db}"
DB_USER="${DB_USER:-postgres}"

if [[ -z "${DB_PASSWORD:-}" ]]; then
    log_error "Переменная DB_PASSWORD не задана. Восстановление невозможно."
    exit 1
fi

export PGPASSWORD="$DB_PASSWORD"

# ─── Предупреждение ───────────────────────────────────────────────────────────
log_warn "========================================================"
log_warn "  ВНИМАНИЕ! Сейчас будет выполнено ПОЛНОЕ ВОССТАНОВЛЕНИЕ"
log_warn "  Это УДАЛИТ все существующие данные в базе '${DB_NAME}'!"
log_warn ""
log_warn "  Источник дампа: $(basename "$DUMP_FILE")"
log_warn "  Целевая БД:     ${DB_USER}@${DB_HOST}:${DB_PORT}/${DB_NAME}"
log_warn "========================================================"
echo ""
read -rp "Вы уверены? Введите 'yes' для подтверждения: " CONFIRM

if [[ "$CONFIRM" != "yes" ]]; then
    log_info "Восстановление отменено пользователем."
    exit 0
fi

# ─── 1. УДАЛЕНИЕ И ПЕРЕСОЗДАНИЕ БАЗЫ ДАННЫХ ──────────────────────────────────
log_info "========================================================"
log_info "  Toy-Warehouse — Восстановление из резервной копии"
log_info "  Дата: $(date '+%Y-%m-%d %H:%M:%S')"
log_info "========================================================"

log_info "Удаление существующей базы данных '${DB_NAME}'..."

# Отключаем все подключения к БД перед удалением
psql \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --dbname="postgres" \
    --no-password \
    --command="SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='${DB_NAME}' AND pid <> pg_backend_pid();" \
    > /dev/null 2>&1 || true

dropdb \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --if-exists \
    --no-password \
    "$DB_NAME"

log_ok "База данных '${DB_NAME}' удалена."

log_info "Создание новой базы данных '${DB_NAME}'..."
createdb \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --no-password \
    "$DB_NAME"

log_ok "База данных '${DB_NAME}' создана."

# ─── 2. ВОССТАНОВЛЕНИЕ ИЗ ДАМПА ──────────────────────────────────────────────
log_info "Восстановление данных из дампа: $(basename "$DUMP_FILE")"
DUMP_SIZE=$(du -sh "$DUMP_FILE" | cut -f1)
log_info "  Размер файла дампа: ${DUMP_SIZE}"

if gunzip -c "$DUMP_FILE" | psql \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --dbname="$DB_NAME" \
    --no-password \
    --quiet \
    > /dev/null 2>&1; then

    log_ok "Данные восстановлены успешно!"
else
    log_error "ОШИБКА при восстановлении данных из дампа!"
    log_error "Проверьте целостность файла: ${DUMP_FILE}"
    exit 1
fi

# ─── 3. ВОССТАНОВЛЕНИЕ МЕДИА-ФАЙЛОВ (если есть архив) ────────────────────────
UPLOADS_DIR="${PROJECT_DIR}/uploads"
UPLOADS_BACKUP_DIR="${PROJECT_DIR}/backups/uploads"

# Ищем последний архив uploads, если путь не указан явно
if [[ -d "$UPLOADS_BACKUP_DIR" ]]; then
    LATEST_UPLOADS=$(find "$UPLOADS_BACKUP_DIR" -name "*.tar.gz" | sort -r | head -1)
    if [[ -n "$LATEST_UPLOADS" ]]; then
        log_info "Восстановление медиа-файлов из: $(basename "$LATEST_UPLOADS")"
        mkdir -p "$UPLOADS_DIR"
        tar -xzf "$LATEST_UPLOADS" -C "$(dirname "$UPLOADS_DIR")" 2>/dev/null || true
        log_ok "Медиа-файлы восстановлены."
    else
        log_warn "Архив uploads/ не найден. Пропускаем восстановление медиа-файлов."
    fi
fi

# ─── 4. ИТОГ ──────────────────────────────────────────────────────────────────
unset PGPASSWORD

log_info "========================================================"
log_ok "Восстановление системы завершено успешно!"
log_info ""
log_info "  Следующие шаги:"
log_info "  1. Запустите API: cd WarehouseAPI && dotnet run"
log_info "     или: docker-compose up -d --build"
log_info "  2. Проверьте работоспособность: http://localhost:5023/health"
log_info "  3. Откройте Swagger UI: http://localhost:5023/swagger"
log_info "========================================================"

exit 0
