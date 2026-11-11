# CodeStyle

CodeStyle is a development utility shared by this repository's contributors and CI. It uses the .NET SDK's Roslyn libraries, the standard C# style fixes selected in `.editorconfig`, and the additional layout and naming rules configured there. It has no application dependencies or migration snapshots.

## Commands

Use the .NET 10 SDK selected by `global.json`. From the repository root:

```powershell
dotnet restore BundleManager.sln
dotnet run --project tools/CodeStyle -- check
dotnet run --project tools/CodeStyle -- fix
dotnet run --project tools/CodeStyle.Tests
```

`check` does not modify source files. `fix` applies standard C# code-style fixes and whitespace changes, then reports any remaining naming violations. Naming changes require a symbol rename in your editor so that references and contracts can be reviewed. The custom formatter verifies that its changes preserve code tokens and literal values.

Both commands return `0` when clean, `1` for remaining style violations, and `2` for invalid arguments, SDK/workspace errors, or an unsafe formatting transformation. Restore the selected workspace before running a check; the tool does not restore application packages implicitly.

The default workspace is the nearest solution or project. You can select another restored workspace and restrict the files checked:

```powershell
dotnet run --project tools/CodeStyle -- check --path BundleManager.sln --include BaseHandlers
dotnet run --project tools/CodeStyle -- fix --path ../AnotherProject/AnotherProject.sln
```

Paths following `--path` and `--include` are relative to the working directory. Files linked from outside the selected workspace directory, `bin`, `obj`, and `artifacts` are excluded. Generated and designer files are skipped unless `codestyle_format_generated_code = true` enables layout checks for them. This repository enables those checks for every checked-in `.Designer.cs` file. Generated files keep their code tokens and names; standard C# style fixes and naming checks only apply to maintained code. Designer event bindings are also read to recognize handler names.

## Configuration

Standard C# preferences remain in `.editorconfig`. Diagnostics explicitly enabled there at `suggestion`, `warning`, or `error` are checked through `dotnet format style`. `IDE1006` is handled by the custom naming checks so that event handlers and original game names retain their exceptions.

These additional options default to the repository's conventions and can be overridden in any matching EditorConfig section:

| Option | Default | Effect |
| --- | --- | --- |
| `codestyle_inline_auto_properties` | `true` | Keep auto-properties inline with their names, including accessor visibility. |
| `codestyle_expand_nonempty_blocks` | `true` | Expand nonempty blocks and place embedded statements on separate lines. |
| `codestyle_expand_initializers` | `true` | Put initializer braces and individual members on separate lines. |
| `codestyle_ternary_new_lines` | `true` | Put `?` and `:` on separate continuation lines, one level per nesting level. |
| `codestyle_blank_line_before_comments` | `true` | Separate standalone comment groups from earlier code; keep first-in-block and consecutive comments together. |
| `codestyle_separate_logical_steps` | `true` | Separate members, local functions, control-flow steps, and completed initializer declarations. |
| `codestyle_check_naming` | `true` | Check visibility-based casing and configured acronyms. |
| `codestyle_acronyms` | `RGB, HSV, CRC, PVS, ID` | Capitalize these acronym segments; initial camelCase segments remain lowercase. |
| `codestyle_preserved_words` | `CgsID` | Keep these explicit word spellings within identifiers. |
| `codestyle_preserve_winforms_handlers` | `true` | Preserve `controlName_EventName` handlers; use a known control's exact casing. |
| `codestyle_preserve_member_names` | `false` | Exempt fields, properties, and enum values that come from an existing format or contract. |
| `codestyle_identifier_exceptions` | empty | Exempt specific identifier names or wildcard patterns, separated by commas or semicolons. |
| `codestyle_exclude` | `false` | Skip a matching source file, including generated files opted into layout checks. |
| `codestyle_format_generated_code` | `false` | Apply token-preserving layout and encoding checks to generated or designer files. Skip naming checks and standard C# style fixes in these files. |

The formatter also uses EditorConfig indentation, line-ending, and UTF-8 BOM preferences. It normalizes line endings in comments and preprocessor directives, trims trailing whitespace in comments, and respects `dotnet_style_allow_multiple_blank_lines_experimental`. String literal values are preserved. Comments inside an accessor prevent automatic property collapsing, so the explanation is preserved.

For an original game model, add an exception before editing or introducing a naming check:

```editorconfig
[Formats/OriginalGameData.cs]
codestyle_preserve_member_names = true
dotnet_naming_rule.visible_fields_are_pascal_case.severity = none
dotnet_naming_rule.properties_are_pascal_case.severity = none
```

For specific native or third-party identifiers:

```editorconfig
[Interop/NativeMethods.cs]
codestyle_identifier_exceptions = WM_*, FILE_*
```

Keep exceptions narrow. Method helpers, parameters, and locals in game-model files are still checked. Symbol renaming and backup-dependent restoration are deliberately absent from the tool.

## CI

The `Style` job in `.github/workflows/build.yml` runs the tool's regression tests and then `check`. Configure that job as a required status check in the repository's branch rules to require it before merging. Running CI does not change those repository settings.

The tool tests cover layout, comments and literals, repeated formatting, configurable indentation, naming exceptions, and command exit codes. Their temporary project needs no external packages.
