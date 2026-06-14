#!/usr/bin/env bash
# =============================================================================
# backup.sh — Скрипт автоматизированного резервного копирования
#             системы Toy-Warehouse (PostgreSQL + uploads)
#
# Использование:
#   ./scripts/backup.sh
#
# Для запуска по расписанию добавьте в crontab (crontab -e):
#   0 2 * * * /opt/warehouse/scripts/backup.sh >> /opt/warehouse/backups/backup.log 2>&1
#   (каждый день в 02:00 ночи)
#
# Требования:
#   - pg_dump (из пакета postgresql-client)
#   - Переменные окружения: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD
#     (загружаются из ../.env если файл существует)
# =============================================================================

set -euo pipefail  # выход при ошибке, незаявленных переменных, ошибках в pipe

# ─── Цвета для вывода в консоль ───────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ─── Функция логирования ──────────────────────────────────────────────────────
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

# ─── Директории ───────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
BACKUP_DIR="${PROJECT_DIR}/backups"
DB_BACKUP_DIR="${BACKUP_DIR}/db"
UPLOADS_BACKUP_DIR="${BACKUP_DIR}/uploads"
LOG_FILE="${BACKUP_DIR}/backup.log"

# ─── Параметры ротации ────────────────────────────────────────────────────────
RETENTION_DAYS=7        # удалять резервные копии старше 7 дней
MAX_BACKUPS_COUNT=30    # максимальное количество хранимых дампов БД

# ─── Загрузка переменных окружения из .env ───────────────────────────────────
ENV_FILE="${PROJECT_DIR}/WarehouseAPI/.env"
if [[ -f "$ENV_FILE" ]]; then
    log_info "Загрузка конфигурации из ${ENV_FILE}"
    # shellcheck disable=SC1090
    set -a
    source "$ENV_FILE"
    set +a
else
    log_warn "Файл .env не найден: ${ENV_FILE}. Используются переменные среды."
fi

# ─── Параметры подключения к БД ───────────────────────────────────────────────
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-warehouse_db}"
DB_USER="${DB_USER:-postgres}"
# DB_PASSWORD берётся из переменной окружения (не выводим в лог!)

if [[ -z "${DB_PASSWORD:-}" ]]; then
    log_error "Переменная DB_PASSWORD не задана. Резервное копирование невозможно."
    exit 1
fi

# Передаём пароль через переменную окружения PGPASSWORD (стандартный способ)
export PGPASSWORD="$DB_PASSWORD"

# ─── Метка времени для имён файлов ───────────────────────────────────────────
TIMESTAMP=$(date '+%Y-%m-%d_%H-%M-%S')
DB_DUMP_FILE="${DB_BACKUP_DIR}/warehouse_db_${TIMESTAMP}.sql.gz"
UPLOADS_ARCHIVE="${UPLOADS_BACKUP_DIR}/uploads_${TIMESTAMP}.tar.gz"
UPLOADS_DIR="${PROJECT_DIR}/uploads"

# ─── Создание директорий если не существуют ───────────────────────────────────
log_info "========================================================"
log_info "  Toy-Warehouse — Резервное копирование"
log_info "  Дата: $(date '+%Y-%m-%d %H:%M:%S')"
log_info "========================================================"

mkdir -p "$DB_BACKUP_DIR"
mkdir -p "$UPLOADS_BACKUP_DIR"

# ─── 1. РЕЗЕРВНОЕ КОПИРОВАНИЕ БАЗЫ ДАННЫХ ────────────────────────────────────
log_info "Начало резервного копирования PostgreSQL..."
log_info "  Источник: ${DB_USER}@${DB_HOST}:${DB_PORT}/${DB_NAME}"
log_info "  Назначение: ${DB_DUMP_FILE}"

if pg_dump \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --dbname="$DB_NAME" \
    --format=plain \
    --no-password \
    --verbose \
    2>/dev/null \
    | gzip > "$DB_DUMP_FILE"; then

    DUMP_SIZE=$(du -sh "$DB_DUMP_FILE" | cut -f1)
    log_ok "Дамп БД создан успешно: ${DB_DUMP_FILE} (размер: ${DUMP_SIZE})"
else
    log_error "ОШИБКА: не удалось создать дамп базы данных!"
    log_error "Проверьте подключение к PostgreSQL: ${DB_USER}@${DB_HOST}:${DB_PORT}/${DB_NAME}"
    exit 1
fi

# ─── 2. РЕЗЕРВНОЕ КОПИРОВАНИЕ МЕДИА-ФАЙЛОВ (uploads/) ────────────────────────
if [[ -d "$UPLOADS_DIR" ]]; then
    log_info "Начало архивации медиа-файлов..."
    log_info "  Источник: ${UPLOADS_DIR}"
    log_info "  Назначение: ${UPLOADS_ARCHIVE}"

    if tar -czf "$UPLOADS_ARCHIVE" -C "$(dirname "$UPLOADS_DIR")" "$(basename "$UPLOADS_DIR")" 2>/dev/null; then
        ARCHIVE_SIZE=$(du -sh "$UPLOADS_ARCHIVE" | cut -f1)
        log_ok "Архив медиа-файлов создан: ${UPLOADS_ARCHIVE} (размер: ${ARCHIVE_SIZE})"
    else
        log_warn "Не удалось создать архив медиа-файлов. Продолжаем без него."
    fi
else
    log_warn "Директория uploads/ не найдена (${UPLOADS_DIR}). Пропускаем архивацию медиа-файлов."
fi

# ─── 3. РОТАЦИЯ СТАРЫХ РЕЗЕРВНЫХ КОПИЙ ───────────────────────────────────────
log_info "Ротация резервных копий старше ${RETENTION_DAYS} дней..."

DELETED_COUNT=0

# Удаляем старые дампы БД
while IFS= read -r old_file; do
    rm -f "$old_file"
    log_info "  Удалён устаревший дамп: $(basename "$old_file")"
    ((DELETED_COUNT++))
done < <(find "$DB_BACKUP_DIR" -name "*.sql.gz" -mtime +"$RETENTION_DAYS")

# Удаляем старые архивы uploads
while IFS= read -r old_file; do
    rm -f "$old_file"
    log_info "  Удалён устаревший архив: $(basename "$old_file")"
    ((DELETED_COUNT++))
done < <(find "$UPLOADS_BACKUP_DIR" -name "*.tar.gz" -mtime +"$RETENTION_DAYS" 2>/dev/null)

if [[ $DELETED_COUNT -eq 0 ]]; then
    log_info "  Устаревших резервных копий не найдено."
else
    log_ok "  Удалено устаревших копий: ${DELETED_COUNT}"
fi

# ─── 4. СТАТИСТИКА ────────────────────────────────────────────────────────────
TOTAL_DB_BACKUPS=$(find "$DB_BACKUP_DIR" -name "*.sql.gz" | wc -l)
TOTAL_SIZE=$(du -sh "$BACKUP_DIR" 2>/dev/null | cut -f1)

log_info "========================================================"
log_ok "Резервное копирование завершено успешно!"
log_info "  Всего дампов БД хранится: ${TOTAL_DB_BACKUPS}"
log_info "  Общий размер директории backups/: ${TOTAL_SIZE}"
log_info "  Последний дамп: $(basename "$DB_DUMP_FILE")"
log_info "========================================================"

# Сбрасываем PGPASSWORD из переменных окружения
unset PGPASSWORD

exit 0
