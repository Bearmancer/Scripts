"""Structural pattern matching example (Python 3.10+)."""

from enum import Enum
from dataclasses import dataclass


class EventType(Enum):
    """Event type enumeration."""

    USER_CREATED = "user_created"
    USER_DELETED = "user_deleted"
    ORDER_PLACED = "order_placed"


@dataclass
class Event:
    """Event data structure."""

    type: EventType
    user_id: int | None = None
    order_id: int | None = None
    amount: float | None = None


def handle_event(event: Event) -> str:
    """
    Handle event using structural pattern matching.

    Args:
        event: Event object with type and data.

    Returns:
        Result string describing the action taken.

    Examples:
        >>> event = Event(EventType.USER_CREATED, user_id=42)
        >>> handle_event(event)
        'Created user 42'
    """
    match event:
        case Event(type=EventType.USER_CREATED, user_id=uid):
            return f"Created user {uid}"

        case Event(type=EventType.USER_DELETED, user_id=uid):
            return f"Deleted user {uid}"

        case Event(type=EventType.ORDER_PLACED, order_id=oid, amount=amt):
            return f"Placed order {oid} for ${amt:.2f}"

        case _:
            return "Unknown event"


def process_response(data: dict) -> str:
    """
    Process API response using pattern matching.

    Args:
        data: Response dictionary.

    Returns:
        Processed result string.
    """
    match data:
        case {"status": "success", "data": {"user_id": uid, "email": email}}:
            return f"Created user {uid} ({email})"

        case {"status": "error", "error": {"code": code, "message": msg}}:
            return f"Error {code}: {msg}"

        case {"status": "success", "data": list() as items}:
            return f"Retrieved {len(items)} items"

        case _:
            return "Invalid response format"
