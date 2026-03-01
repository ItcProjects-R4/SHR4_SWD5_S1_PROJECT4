using LibraryMangementBLL;
using ITCGroup04LibraryMangementPL.Models;

// ================================================
// Services (BLL)
// ================================================
var bookService   = new BookService();
var memberService = new MemberService();
var borrowService = new BorrowService();

// ================================================
// Main Loop Menu
// ================================================
while (true)
{
    ShowMainMenu();
    int choice = ReadIntInRange(1, 9);
    Console.Clear();

    switch (choice)
    {
        case 1: ShowAllBooks();       break;
        case 2: ShowAvailableBooks(); break;
        case 3: ShowAllMembers();     break;
        case 4: ShowActiveBorrows();  break;
        case 5: HandleAddBook();      break;
        case 6: HandleDeleteBook();   break;
        case 7: HandleAddMember();    break;
        case 8: HandleBorrowBook();   break;
        case 9: HandleReturnBook();   break;
    }

    if (choice == 9) break;

    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey();
    Console.Clear();
}

// ================================================
// Display Methods
// ================================================

void ShowAllBooks()
{
    PrintHeader("All Books");
    var books = bookService.GetAllBooks();
    if (books.Count == 0) { Console.WriteLine("  No books found."); return; }

    foreach (var b in books)
    {
        var book = new Book(b.Id, b.Title, b.Author, b.IsAvailable);
        Console.WriteLine(book);
    }
}

void ShowAvailableBooks()
{
    PrintHeader("Available Books  [View: AvailableBooks]");
    var books = bookService.GetAvailableBooks();
    if (books.Count == 0) { Console.WriteLine("  No available books."); return; }

    foreach (var b in books)
    {
        var book = new Book(b.Id, b.Title, b.Author, isAvailable: true);
        Console.WriteLine(book);
    }
}

void ShowAllMembers()
{
    PrintHeader("All Members");
    var members = memberService.GetAllMembers();
    if (members.Count == 0) { Console.WriteLine("  No members found."); return; }

    foreach (var m in members)
        Console.WriteLine($"  [{m.Id}] {m.FullName} — {m.Phone}");
}

void ShowActiveBorrows()
{
    PrintHeader("Active Borrows (Not Returned Yet)");
    var borrows = borrowService.GetActiveBorrows();
    if (borrows.Count == 0) { Console.WriteLine("  No active borrows."); return; }

    foreach (var b in borrows)
        Console.WriteLine($"  [BorrowID:{b.BorrowId}] '{b.BookTitle}'" +
                          $" by {b.MemberName} | Since: {b.BorrowDate.ToShortDateString()}");
}

// ================================================
// Action Methods
// ================================================

void HandleAddBook()
{
    PrintHeader("Add New Book");
    string title  = ReadNonEmptyString("Book Title  : ");
    string author = ReadNonEmptyString("Book Author : ");

    var (success, message) = bookService.AddBook(title, author);
    PrintResult(success, message);
}

void HandleDeleteBook()
{
    PrintHeader("Delete Book");
    ShowAllBooks();
    int id = ReadPositiveInt("\nEnter Book ID to delete: ");

    var (success, message) = bookService.DeleteBook(id);
    PrintResult(success, message);
}

void HandleAddMember()
{
    PrintHeader("Add New Member");
    string fullName = ReadNonEmptyString("Full Name : ");
    string phone    = ReadNonEmptyString("Phone     : ");

    var (success, message) = memberService.AddMember(fullName, phone);
    PrintResult(success, message);
}

void HandleBorrowBook()
{
    PrintHeader("Borrow a Book  [SP: SP_BorrowBook + Trigger]");

    Console.WriteLine("--- Available Books ---");
    ShowAvailableBooks();

    Console.WriteLine("\n--- Members ---");
    ShowAllMembers();

    int bookId   = ReadPositiveInt("\nEnter Book ID   : ");
    int memberId = ReadPositiveInt("Enter Member ID : ");

    var (success, message) = borrowService.BorrowBook(bookId, memberId);
    PrintResult(success, message);
}

void HandleReturnBook()
{
    PrintHeader("Return a Book  [SP: SP_ReturnBook]");

    Console.WriteLine("--- Active Borrows ---");
    ShowActiveBorrows();

    int borrowId = ReadPositiveInt("\nEnter Borrow ID to return: ");

    var (success, message) = borrowService.ReturnBook(borrowId);
    PrintResult(success, message);
}

// ================================================
// UI Helpers
// ================================================

void ShowMainMenu()
{
    Console.WriteLine("╔════════════════════════════════════════╗");
    Console.WriteLine("║       ITC Library Management System    ║");
    Console.WriteLine("╠════════════════════════════════════════╣");
    Console.WriteLine("║  1 - Show All Books                    ║");
    Console.WriteLine("║  2 - Show Available Books  (View)      ║");
    Console.WriteLine("║  3 - Show All Members                  ║");
    Console.WriteLine("║  4 - Show Active Borrows               ║");
    Console.WriteLine("║  5 - Add Book                          ║");
    Console.WriteLine("║  6 - Delete Book                       ║");
    Console.WriteLine("║  7 - Add Member                        ║");
    Console.WriteLine("║  8 - Borrow Book  (SP + Trigger)       ║");
    Console.WriteLine("║  9 - Return Book  (SP)                 ║");
    Console.WriteLine("╚════════════════════════════════════════╝");
    Console.Write("Your Choice (1-9): ");
}

void PrintHeader(string title) =>
    Console.WriteLine($"\n=== {title.ToUpper()} ===\n");

void PrintResult(bool success, string message) =>
    Console.WriteLine(success ? $"✔ {message}" : $"✘ {message}");

int ReadIntInRange(int min, int max)
{
    int result;
    while (!int.TryParse(Console.ReadLine(), out result) || result < min || result > max)
        Console.Write($"Invalid. Enter ({min}-{max}): ");
    return result;
}

int ReadPositiveInt(string prompt)
{
    int result;
    Console.Write(prompt);
    while (!int.TryParse(Console.ReadLine(), out result) || result <= 0)
        Console.Write("Invalid. Enter positive number: ");
    return result;
}

string ReadNonEmptyString(string prompt)
{
    string? input;
    Console.Write(prompt);
    while (string.IsNullOrWhiteSpace(input = Console.ReadLine()))
        Console.Write("Cannot be empty. Try again: ");
    return input;
}
