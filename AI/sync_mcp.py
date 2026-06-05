#!/usr/bin/env python3
"""Test all MCP server connections."""

import subprocess
import json
import sys
import urllib.request
import urllib.error


def test_docker_mcp():
    """Test Docker MCP gateway."""
    try:
        result = subprocess.run(
            ["docker", "mcp", "gateway", "list"],
            capture_output=True, text=True, timeout=10
        )
        return result.returncode == 0, result.stdout.strip() or result.stderr.strip()
    except Exception as e:
        return False, str(e)


def test_crawl4ai():
    """Test crawl4ai SSE endpoint."""
    try:
        req = urllib.request.Request("http://localhost:11235/", method="HEAD")
        resp = urllib.request.urlopen(req, timeout=5)
        return True, f"Status: {resp.status}"
    except urllib.error.HTTPError as e:
        return True, f"Status: {e.code} (server responding)"
    except Exception as e:
        return False, str(e)


def test_playwright():
    """Test playwright MCP."""
    try:
        result = subprocess.run(
            ["npx", "-y", "@playwright/mcp@latest", "--help"],
            capture_output=True, text=True, timeout=30
        )
        return result.returncode == 0, "Available"
    except Exception as e:
        return False, str(e)


def test_agentql():
    """Test agentql MCP."""
    try:
        result = subprocess.run(
            ["npx", "-y", "agentql-mcp", "--help"],
            capture_output=True, text=True, timeout=30
        )
        return result.returncode == 0, "Available"
    except Exception as e:
        return False, str(e)


def main():
    tests = [
        ("Docker MCP", test_docker_mcp),
        ("crawl4ai", test_crawl4ai),
        ("Playwright", test_playwright),
        ("AgentQL", test_agentql),
    ]

    print("MCP Connection Status\n" + "=" * 40)
    all_ok = True

    for name, test_fn in tests:
        ok, msg = test_fn()
        status = "✓" if ok else "✗"
        print(f"{status} {name}: {msg}")
        if not ok:
            all_ok = False

    print("=" * 40)
    if all_ok:
        print("All services ready. Restart opencode to connect.")
    else:
        print("Some services need attention.")
    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
