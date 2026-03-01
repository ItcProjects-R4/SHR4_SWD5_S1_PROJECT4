namespace ITCGroup04LibraryMangementPL.Models
{
    public class Book
    {
        public int    BookID      { get; set; }
        public string Title       { get; set; }
        public string Author      { get; set; }
        public bool   IsAvailable { get; set; }

        public Book(int bookId, string title, string author, bool isAvailable = true)
        {
            BookID      = bookId;
            Title       = title;
            Author      = author;
            IsAvailable = isAvailable;
        }

        public override string ToString()
        {
            string status = IsAvailable ? " Available" : " Borrowed";
            return $"  [{BookID}] {Title} — {Author} | {status}";
        }
    }
}
