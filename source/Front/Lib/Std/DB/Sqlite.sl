public class DB.Sqlite3
{
    public class Connection
    {
        public class Command
        {
            public void executeNonQuery()
            {
            }
        }
        public Cursor execute( string sqlstr )
        {
            return new Cursor();
        }
        public void execute( string sqlstr, Tulple args )
        {
        }
        public void commit()
        {
        }
        public void close()
        {
        }
    }
    public class Cursor
    {

    }
    static Connection connect(string path)
    {
        return new Connection();
    }
}