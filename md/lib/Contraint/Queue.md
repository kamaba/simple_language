# Queue（队列）

FIFO（先进先出）集合。支持入队、出队与查看头部元素。

API：
- `enqueue(x)`, `dequeue()`, `peek()`, `isEmpty`, `length`

示例：

```s
var q = Queue<int>();
q.enqueue(1);
var v = q.dequeue();
```

