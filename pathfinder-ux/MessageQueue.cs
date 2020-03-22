using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder
{
    using System.Threading;
    
    sealed class MessageQueue
    {
        private Queue<Message> q;
        private Mutex mu;

        private MessageQueue()
        {
            q = new Queue<Message>();
            mu = new Mutex();
        }

        private static MessageQueue singleton = new MessageQueue();

        public static MessageQueue GetInstance()
        {
            return singleton;
        }

        public void Submit(Message a) 
        {
            mu.WaitOne();
            q.Enqueue(a);
            mu.ReleaseMutex();
        }

        public Message[] CheckOut() 
        {
            mu.WaitOne();
            var res = q.ToArray(); // 将q中所有元素「复制」到新数组
            q.Clear();
            mu.ReleaseMutex();
            return res;
        }
    }
}
