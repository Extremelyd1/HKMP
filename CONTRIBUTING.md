# Contributing to this repository
## Issues
If you come across a bug or have an idea of a feature you want to see, feel free to open an issue.
Please check before opening a new issue whether the same thing has not already been proposed by someone else.

Your issue can take a few different forms:
- A bug report, if something isn't working how it should.
- An enhancement, if you want to propose a new feature or implementation idea.
- Question, if you are unsure about a certain aspect of the repository or the codebase and want clarification.

In any of these cases it is best if you provide as much detail as possible about your issue.  
In case of a bug report this could include logs, steps to reproduce and an explanation of what is happening and what you expected to happen.  
In case of an enhancement you could think about what underlying systems need to change/be implemented to accomodate the feature.

## Pull requests
Contributions in the form of pull requests are accepted, but keep in mind that the quality and maintainability of the project is the most important.
Therefore, you should adhere to the existing code style, which is described [in a later section](https://github.com/Extremelyd1/HKMP/blob/master/CONTRIBUTING.md#styleguide).
Commenting your code is required, although you don't have provide documentation for self-explanatory or trivial pieces of code.

In addition to this you should make sure that you supply a clear and concise explanation of what your pull request entails.
From this explanation one should be able to understand the work you have done, what it accomplishes and how it functions.

Lastly, pull requests should not be too long.
Try to limit the scope of your pull request to a single feature/change, or in case this is also too large, to a single sub-system of the feature/change you want to see.

### Styleguide
To keep this section from getting unnecessarily large only the most important styles are included.
An EditorConfig containing the same style described below is also [present in the project](https://github.com/Extremelyd1/HKMP/blob/master/.editorconfig).
Note that this EditorConfig is exported from Jetbrains Rider, so it might not work for all settings in other IDEs.

Indentation:
- Spaces for indentation
- Indent size: `4`

Naming:
- Types and namespaces: `UpperCamelCase`
- Interface: `IUpperCamelCase`
- Type parameters: `TUpperCamelCase`
- Methods: `UpperCamelCase`
- Properties: `UpperCamelCase`
- Events: `UpperCamelCase`
- Local variables: `lowerCamelCase`
- Local constant: `lowerCamelCase`
- Parameters: `lowerCamelCase`
- Fields (not private): `UpperCamelCase`
- Instance fields (private): `_lowerCamelCase`
- Static field (private): `_lowerCamelCase`
- Constant fields: `UpperCamelCase`
- Static readonly fields: `UpperCamelCase`
- Enum members: `UpperCamelCase`
- Local functions: `UpperCamelCase`
- All other entities: `UpperCamelCase`

Syntax style:
- Use `var` where possible, unless context lacks proper clarity
- Explicitly denote `private` for type members and `internal` for types in relevant cases

Braces:
- Always use braces for `if`, `for`, `while` and similar statements
- Braces should always be placed at the end of the line (K&R style)

Spaces:
- Use spaces around the parenthesis of statements like: `if`, `for`, and `while`
- Don't use spaces within parenthesis of statements like: `if`, `for`, and `while`
- Use spaces around all binary operators (`=`, `+`, `>>`, etc.)
- Don't use spaces before commas, but do use spaces after commas

In all other cases, please try to find an example in the existing code and try to adhere to the style used there.
If no such example can be found, use the most logical style for the context.