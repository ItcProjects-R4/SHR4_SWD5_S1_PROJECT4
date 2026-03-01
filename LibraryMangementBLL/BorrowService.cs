using LibraryMangementDAL;

namespace LibraryMangementBLL
{
    public class BorrowService
    {
        private readonly BorrowDAL _borrowDAL = new BorrowDAL();

        // ================================================
        // Borrow Book
        // ================================================
        public (bool Success, string Message) BorrowBook(int bookId, int memberId)
        {
            if (bookId <= 0)   return (false, "Invalid Book ID.");
            if (memberId <= 0) return (false, "Invalid Member ID.");

            return _borrowDAL.BorrowBook(bookId, memberId);
        }

        // ================================================
        // Return Book
        // ================================================
        public (bool Success, string Message) ReturnBook(int borrowId)
        {
            if (borrowId <= 0) return (false, "Invalid Borrow ID.");

            return _borrowDAL.ReturnBook(borrowId);
        }

        // ================================================
        // Get Active Borrows
        // ================================================
        public List<(int BorrowId, string BookTitle, string MemberName, DateTime BorrowDate)> GetActiveBorrows()
        {
            return _borrowDAL.GetActiveBorrows();
        }
    }
}
