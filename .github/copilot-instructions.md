# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction

## Code Style
- Use specific formatting rules
- Follow naming conventions

## Project-Specific Rules
- VM 项目需要保持独立运行（不直接依赖 Front 项目/内存对象）
- 语言级 Assembly/Module/IR 信息应通过导出/导入边界传递（例如 JSON/二进制包），而不是给 VM 加 ProjectReference 到 Front。