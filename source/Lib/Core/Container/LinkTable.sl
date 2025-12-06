

class Node{
  value = null;
  Node next = null
  
  Node( _val, Node _next )
  {

  }
}

public class LinkTable extends Object
{
    Node node = null;    

    
    public void Add( t )
    {
        if( node == null )
        {
            node = Node()
            node.value = t 
        }
        else
        {
            var next = Node()
            next.value = t 

            node.next = next;
        }
    }
}

class Node<T> {
  T value;
  Node<T> next; // 空安全（Dart 2.12+）
  
  Node(this.value, [this.next]);
}
public class LinkTable<T> extends Object
{
    Node<T> node = null;

    public void Add( T t )
    {
        if( node == null )
        {
            node = Node<T>()
            node.value = t 
        }
        else
        {
            var next = Node<T>()
            next.value = t 

            node.next = next;
        }
    }
}