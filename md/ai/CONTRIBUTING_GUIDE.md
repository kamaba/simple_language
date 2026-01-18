# Contributing Guide for SimpleLanguage (AI-aware)

This guide helps contributors and AI automation workflows to make consistent changes.

1. Code style & conventions
   - Target: .NET 6, C# 10 for runtime code.
   - Keep existing code formatting; prefer existing naming patterns.
   - For language/stdlib (.sl files), follow existing syntax used in `source/Lib/Core`.

2. Making changes
   - Small PRs: change at most 1-3 files per PR for reviewability.
   - Add tests (scripts in `test/` or `samples/`) demonstrating expected behavior.
   - Run `dotnet build SimpleLanguage.csproj` to ensure no compile errors.

3. AI-assisted changes
   - When using AI to generate code or docs, include the relevant file snippets and a clear high-level instruction.
   - Always run build and tests after AI-generated changes before creating PR.

4. Commit messages
   - Provide short summary + reason. Example: `Fix: treat 'abstract' as token; implement abstract-method checks`.

5. PR review checklist
   - Build passes.
   - Tests or samples added/updated for behavior changes.
   - Documentation updated if public behavior changed.

6. Where to add docs
   - Global product docs: `md/ai/PRD.md`
   - AI prompts and guides: `md/ai/` (per component or policy)
   - Module-level docs: add `README.md` under the relevant `source/*` directory.

7. CI & automation
   - Consider adding GitHub Action to build + run sample scripts + regenerate docs (optional).

Thanks for contributing! Keep changes small and reviewable; AI is a helper ¡ª humans remain the final reviewer.