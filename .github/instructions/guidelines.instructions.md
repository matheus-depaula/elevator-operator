---
applyTo: "**"
---

## Project Context

The Elevator Operator project is a .NET 9-based application designed to manage elevator operations within a building. It follows clean architecture principles, separating concerns into distinct layers: Domain, Application, Infrastructure, and CLI.

For full architecture details, see [`docs/elevator-system-design.md`](../../docs/elevator-system-design.md).

## Coding Guidelines

1. **Follow Clean Architecture**: Adhere to the principles of clean architecture by keeping the layers separate and ensuring that dependencies flow inward.

2. **Use Meaningful Names**: Choose clear and descriptive names for variables, methods, and classes to convey their purpose and usage.

3. **Implement Error Handling**: Use custom exceptions to handle errors gracefully. Ensure that all public methods validate inputs and handle potential errors.

4. **Write Unit Tests**: Create unit tests for all critical components, especially for business logic in the Domain layer. Use xUnit and Moq for testing.

5. **Document Code**: Use XML comments to document public APIs and complex logic. Maintain up-to-date README files and architecture diagrams.

6. **Optimize for Performance**: Be mindful of performance implications, especially in the Domain layer. Use asynchronous programming and efficient data structures where appropriate.

7. **Review and Refactor**: Regularly review code for improvements. Refactor code to reduce complexity and improve readability.

8. **Adhere to Style Conventions**: Follow .NET coding conventions for formatting, naming, and structuring code. Use tools like StyleCop or EditorConfig to enforce consistency.

9. **Use Dependency Injection**: Leverage dependency injection for managing dependencies, especially in the Application and Infrastructure layers.
