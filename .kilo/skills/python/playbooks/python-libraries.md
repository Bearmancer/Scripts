# Python Library Standards

This playbook is the authoritative reference for library selection decisions in Python projects. Use this matrix to choose the right tool for each domain.

## Library Matrix

| Purpose         | Library                                       |
| --------------- | --------------------------------------------- |
| CLI             | `typer` or `click`                            |
| Logging         | `structlog` or `loguru`                       |
| Resiliency      | `tenacity`                                    |
| Test Framework  | `pytest`                                      |
| Assertions      | Built-in `assert` or `pytest` matchers        |
| Mocking         | `pytest-mock`                                 |
| HTTP Client     | `httpx`                                       |
| JSON            | `pydantic` for validation, `orjson` for speed |
| CSV             | `polars` or `pandas`                          |
| Data Validation | `pydantic`                                    |
| Web Framework   | `FastAPI` or `Django`                         |
| ORM             | `SQLAlchemy 2.0` or `Django ORM`              |
| Task Queue      | `celery` or `rq`                              |
| Async           | `asyncio` + `aiohttp` or `httpx`              |
| Linting         | `ruff`                                        |
| Formatting      | `ruff format`                                 |
