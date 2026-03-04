# 📚 Library Management System.

> A console-based Library Management System built with **C# .NET 8**, following **3-Tier Architecture** and **OOP** principles, connected to **SQL Server** via **ADO.NET**.

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│         ITCGroup04LibraryMangementPL                │
│              Presentation Layer                     │
│     Program.cs  │  Models/Book.cs  │  Status.cs    │
└────────────────────────┬────────────────────────────┘
                         │ calls
┌────────────────────────▼────────────────────────────┐
│               LibraryMangementBLL                   │
│             Business Logic Layer                    │
│   BookService  │  MemberService  │  BorrowService  │
└────────────────────────┬────────────────────────────┘
                         │ calls
┌────────────────────────▼────────────────────────────┐
│               LibraryMangementDAL                   │
│               Data Access Layer                     │
│   DBHelper  │  BookDAL  │  MemberDAL  │  BorrowDAL │
└────────────────────────┬────────────────────────────┘
                         │ ADO.NET
┌────────────────────────▼────────────────────────────┐
│                LibraryDB (SQL Server)               │
│   Tables: Books, Members, Borrows                   │
│   View: AvailableBooks                              │
│   SPs:  SP_BorrowBook, SP_ReturnBook                │
│   Trigger: TR_PreventUnavailableBorrow              │
└─────────────────────────────────────────────────────┘
```

---

## 📁 Project Structure

```
LibraryMangementSystemSolution/
│
├── ITCGroup04LibraryMangementPL/        ← Startup Project (EXE)
│   ├── Models/
│   │   ├── Book.cs
│   │   └── Status.cs
│   └── Program.cs
│
├── LibraryMangementBLL/                 ← Class Library (DLL)
│   ├── BookService.cs
│   ├── MemberService.cs
│   └── BorrowService.cs
│
├── LibraryMangementDAL/                 ← Class Library (DLL)
│   ├── DBHelper.cs
│   ├── BookDAL.cs
│   ├── MemberDAL.cs
│   └── BorrowDAL.cs
│
├── SQL/
│   └── LibraryDB.sql                   ← Run this first in SSMS
│
└── LibraryMangementSystemSolution.sln
```

---

## ⚙️ Tech Stack

| Technology | Usage |
|---|---|
| C# .NET 8 | Main language |
| SQL Server | Database |
| ADO.NET | Data access (`Microsoft.Data.SqlClient`) |
| Visual Studio 2022 | IDE |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or Developer)
- [SSMS](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) or Azure Data Studio

### 1 — Set up the Database

Open **SSMS**, connect to your server, and run:

```
SQL/LibraryDB.sql
```

This will create:
- `LibraryDB` database
- `Books`, `Members`, `Borrows` tables
- `AvailableBooks` view
- `SP_BorrowBook`, `SP_ReturnBook` stored procedures
- `TR_PreventUnavailableBorrow` trigger
- Sample seed data

### 2 — Update the Connection String

Open `LibraryMangementDAL/DBHelper.cs` and update the server name:

```csharp
// Change . to your SQL Server instance name
"Server=.;Database=LibraryDB;Integrated Security=True;TrustServerCertificate=True;"
```

> **How to find your server name?** Open SSMS — it's shown on the login screen.

### 3 — Run the Project

```bash
# Using .NET CLI
cd ITCGroup04LibraryMangementPL
dotnet run
```

Or press **F5** in Visual Studio with `ITCGroup04LibraryMangementPL` set as the Startup Project.

---

## 🖥️ Menu Options

```
╔════════════════════════════════════════╗
║    📚 ITC Library Management System   ║
╠════════════════════════════════════════╣
║  1 - Show All Books                   ║
║  2 - Show Available Books  (View)     ║
║  3 - Show All Members                 ║
║  4 - Show Active Borrows              ║
║  5 - Add Book                         ║
║  6 - Delete Book                      ║
║  7 - Add Member                       ║
║  8 - Borrow Book  (SP + Trigger)      ║
║  9 - Return Book  (SP)                ║
╚════════════════════════════════════════╝
```

---

## 🗄️ Database Schema

```sql
Books    (BookID PK, Title, Author, IsAvailable BIT)
Members  (MemberID PK, FullName, Phone)
Borrows  (BorrowID PK, BookID FK, MemberID FK, BorrowDate, ReturnDate)
```

### SQL Objects

| Object | Type | Purpose |
|---|---|---|
| `AvailableBooks` | VIEW | Returns only books where `IsAvailable = 1` |
| `SP_BorrowBook` | Stored Procedure | Validates and records a book borrow |
| `SP_ReturnBook` | Stored Procedure | Records a book return |
| `TR_PreventUnavailableBorrow` | Trigger | Blocks borrowing an already-borrowed book |

---

## 👥 Team — ITC Group 04

| Pair | Members | Responsibility |
|---|---|---|
| Pair 1 | Member 1 & 2 | Database — Tables, SPs, View, Trigger |
| Pair 2 | Member 3 & 4 | Data Access Layer (DAL) |
| Pair 3 | Member 5 & 6 | BLL + Models + Presentation Layer |

---

## 🔑 Key OOP & Architecture Concepts Applied

- **3-Tier Architecture** — strict layer separation, PL never touches DAL directly
- **Dependency Injection** — each service receives its DAL dependency via constructor
- **Encapsulation** — all fields are private, exposed through properties
- **Single Responsibility** — each class has one job
- **Parameterized Queries** — no string concatenation, prevents SQL injection
- **Value Tuples** — methods return `(bool Success, string Message)` instead of throwing exceptions

---

## 📝 License

This project was created for educational purposes as part of the ITC training program.
