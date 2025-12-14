import logging
import json
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, cast
from rich.console import Console
from rich.logging import RichHandler


class JsonFileHandler(logging.Handler):
    log_path: Path
    lock_path: Path
    session_id: str
    started_at: str
    session_closed: bool

    def __init__(self, service_name: str) -> None:
        super().__init__()
        self.log_path = Path.home() / ".toolkit" / "logs" / f"{service_name}.json"
        self.lock_path = self.log_path.with_suffix(".lock")
        self.session_id = str(uuid.uuid4())
        self.started_at = datetime.now(timezone.utc).isoformat()
        self.session_closed = False

        self.log_path.parent.mkdir(parents=True, exist_ok=True)
        self._handle_stale_lock()
        self._write_lock()

        self._append_entry(
            {
                "timestamp": self.started_at,
                "type": "session_start",
                "session_id": self.session_id,
            }
        )

    def emit(self, record: logging.LogRecord) -> None:
        log_entry: dict[str, Any] = {
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "type": "log",
            "session_id": self.session_id,
            "level": record.levelname,
            "message": self.format(record),
            "source": {
                "module": record.module,
                "function": record.funcName,
                "line": record.lineno,
            },
        }

        if hasattr(record, "data"):
            log_entry["data"] = getattr(record, "data")

        self._append_entry(log_entry)

    def close(self) -> None:
        if not self.session_closed:
            self._append_entry(
                {
                    "timestamp": datetime.now(timezone.utc).isoformat(),
                    "type": "session_end",
                    "session_id": self.session_id,
                }
            )
            self._delete_lock()
            self.session_closed = True
        super().close()

    def _handle_stale_lock(self) -> None:
        if not self.lock_path.exists():
            return

        lock_content = self.lock_path.read_text(encoding="utf-8")
        stale_session_id, stale_started_at = lock_content.split("|")

        self._append_entry(
            {
                "timestamp": datetime.now(timezone.utc).isoformat(),
                "type": "session_crash",
                "session_id": stale_session_id,
                "started_at": stale_started_at,
            }
        )
        self._delete_lock()

    def _write_lock(self) -> None:
        self.lock_path.write_text(
            f"{self.session_id}|{self.started_at}", encoding="utf-8"
        )

    def _delete_lock(self) -> None:
        if self.lock_path.exists():
            self.lock_path.unlink()

    def _append_entry(self, entry: dict[str, Any]) -> None:
        entries = self._load_entries()
        entries.append(entry)
        self.log_path.write_text(
            json.dumps(entries, indent=2, ensure_ascii=False), encoding="utf-8"
        )

    def _load_entries(self) -> list[dict[str, Any]]:
        if not self.log_path.exists():
            return []
        content = self.log_path.read_text(encoding="utf-8").strip()
        if not content:
            return []
        data = json.loads(content)
        if isinstance(data, list):
            return cast(list[dict[str, Any]], data)
        return []


def configure_logging(service_name: str = "toolkit") -> logging.Logger:
    logger = logging.getLogger("toolkit")
    logger.setLevel(logging.INFO)

    if not logger.handlers:
        console = Console()
        console_handler = RichHandler(
            console=console,
            show_time=True,
            show_path=False,
            rich_tracebacks=True,
            markup=True,
        )
        console_handler.setLevel(logging.INFO)
        logger.addHandler(console_handler)

        json_handler = JsonFileHandler(service_name)
        json_handler.setLevel(logging.INFO)
        logger.addHandler(json_handler)

    return logger


def get_logger(service_name: str = "toolkit") -> logging.Logger:
    return configure_logging(service_name)
