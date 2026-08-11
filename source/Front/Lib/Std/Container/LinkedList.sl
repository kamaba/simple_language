
public class LinkedList extends Object
{    
    class Node
    {
        Object _value = null;
        Node _front = null
        Node _next = null
    
        Node( Object _val )
        {
            this._value = _val
        }
        public void front( Node f )
        {
            this._front = f
        }
        public void next( Node n )
        {
            this._next = n
        }
        public get Object value()
        {
            return this._value
        }
        public get Node front()
        {
            return this._front
        }
        public get Node next()
        {
            return this._next
        }
    }

    Node _root = null;

    public void add( t )
    {
        if( this._root == null )
        {
            this._root = Node()
            node.value = t 
        }
        else
        {
            var next = Node()
            next.value = t 
            node.next = next;
        }
    }
    public void remove( Node rn )
    {

    }
    public Node find( Node n )
    {
        Node find = node
        while true
        {
            if find == null 
            {
                ret find
            }

            if( find == n )
            {
                ret find
            }
            find = find.next
        }
    }
}
