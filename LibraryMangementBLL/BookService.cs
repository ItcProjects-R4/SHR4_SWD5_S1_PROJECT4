using LibraryMangementDAL;

namespace LibraryMangementBLL
{
    public class BookService
    {
        private readonly BookDAL _bookDAL = new BookDAL();

        // ================================================
        // Get All Books
        // ================================================
        public List<(int Id, string Title, string Author, bool IsAvailable)> GetAllBooks()
        {
            return _bookDAL.GetAllBooks();
        }

        // ================================================
        // Get Available Books (via View)
        // ================================================
        public List<(int Id, string Title, string Author)> GetAvailableBooks()
        {
            return _bookDAL.GetAvailableBooks();
        }

        // ================================================
        // Add Book with validation
        // ================================================
        public (bool Success, string Message) AddBook(string title, string author)
        {
            if (string.IsNullOrWhiteSpace(title))
                return (false, "Title cannot be empty.");

            if (string.IsNullOrWhiteSpace(author))
                return (false, "Author cannot be empty.");

            bool result = _bookDAL.AddBook(title, author);
            return result
                ? (true,  "Book added successfully.")
                : (false, "Failed to add book.");
        }

        // ================================================
        // Delete Book with validation
        // ================================================
        public (bool Success, string Message) DeleteBook(int bookId)
        {
            if (bookId <= 0)
                return (false, "Invalid Book ID.");

            bool result = _bookDAL.DeleteBook(bookId);
            return result
                ? (true,  "Book deleted successfully.")
                : (false, "Book not found.");
        }
    }
}
