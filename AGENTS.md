# Инструкции для AI-агента

Проект **mcp-servers-for-revit**: связь Cursor ↔ Revit через MCP.

## Перед задачей по Revit

1. **Обязательно прочитай и выполни** [.cursor/rules/revit-mcp.mdc](.cursor/rules/revit-mcp.mdc).
2. Руководство для пользователей: [docs/user-guide-revit-mcp.md](docs/user-guide-revit-mcp.md).

## Режим по умолчанию (проектировщик)

- Только **MCP-tools** `mcp-server-for-revit`.
- **Не** использовать `send_code_to_revit` без явного согласия пользователя.
- **Не** редактировать `server/`, `plugin/`, `commandset/` при работе с моделью.
- В конце ответа — список использованных MCP-функций.

## Режим разработчика

Только если пользователь явно просит изменить код MCP, плагина или command set.
