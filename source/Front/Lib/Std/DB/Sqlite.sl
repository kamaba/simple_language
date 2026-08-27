public class DB.Sqlite3
{
    public enum SQLITE_OPEN
    {
        READONLY = 1,
        READWRITE = 2,
        CREATE = 4,
        URI = 128,
        MEMORY = 256,
        NOMUTEX = 2048,
        FULLMUTEX = 4096,
        SHAREDCACHE = 8192,
        PRIVATECACHE = 16384,
    }
    public enum SqliteErrorCode
    {
        OK = {code = 0}
        ERROR = {code = 1}
        INTERNAL = {code = 2}
        PERM = {code = 3}
        ABORT = {code = 4}
        BUSY = {code = 5}
        LOCKED = {code = 6}
        NOMEM = {code = 7}
        READONLY = {code = 8}
        INTERRUPT = {code = 9}
        IOERR = {code = 10}
        CORRUPT = {code = 11}
        NOTFOUND = {code = 12}
    }

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
        public void lock()
        {
        }
        public void unlock()
        { 
        }
        public void close()
        {
        }
    }
    public class Cursor
    {

    }
    public class Row
    {

    }
    static Connection connect(string databasepath, float timeout=5.0, detect_types=0, isolation_level="DEFERRED", check_same_thread=true, factory=null, cached_statements=100, uri=false )
    {
        return new Connection();
    }
}