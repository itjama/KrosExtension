"""Read-only Omega legacy string codec; no database access or writes.

Derived from static inspection of signed Omega 29.60.00.00 and verified
against the supplied 29.50.0 database samples. This implements the codec,
not the surrounding application's input cleaning or upper-case conversion.
Windows-1250 matches the supplied Slovak samples; unsupported characters
raise an encoding error rather than being silently replaced.
"""

import re

_MASK = 0x75
_SEED = 0x71


def checksum_byte(payload: bytes) -> int:
    """Return the stored final byte for encoded payload bytes only."""
    return (sum(value ^ _MASK for value in payload) & 0xFF) ^ _MASK


def encode(text: str) -> str:
    """Encode exact input, including checksum; does not uppercase or trim."""
    if not isinstance(text, str):
        raise TypeError("Expected text")
    state = _SEED
    payload = bytearray()
    for value in text.encode("cp1250", errors="strict"):
        state ^= value
        payload.append(state ^ _MASK)
    payload.append(checksum_byte(payload))
    return payload.hex()


def _parse(encoded: str) -> bytes:
    if not isinstance(encoded, str) or not re.fullmatch(r"(?:[0-9A-Fa-f]{2})+", encoded):
        raise ValueError("Expected nonempty, even-length hexadecimal text")
    return bytes.fromhex(encoded)


def is_valid(encoded: str) -> bool:
    """Validate format and checksum without decoding the underlying text."""
    try:
        data = _parse(encoded)
    except ValueError:
        return False
    return checksum_byte(data[:-1]) == data[-1]


def decode(encoded: str) -> str:
    """Decode only after checking the final byte; reject a mismatch."""
    data = _parse(encoded)
    if checksum_byte(data[:-1]) != data[-1]:
        raise ValueError("Omega checksum mismatch")
    previous = _SEED
    plain = bytearray()
    for value in data[:-1]:
        state = value ^ _MASK
        plain.append(state ^ previous)
        previous = state
    return plain.decode("cp1250", errors="strict")
