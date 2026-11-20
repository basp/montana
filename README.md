# Montana
A personal time tracker. Inspired by **Clockify** but obviously without all the
fancy enterprise features.

## ER Diagram
```mermaid
erDiagram
    TimeEntry }o--o{ Tag : "has"
    TimeEntry }o--o| Project : "belongs to"
```