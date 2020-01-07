using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Controller
{
    using Actions;
    using System.Threading;
    
    sealed class ActionQueue
    {
        private Queue<Action> q;
        private Mutex mu;

        private ActionQueue()
        {
            q = new Queue<Action>();
            mu = new Mutex();
        }

        private static ActionQueue singleton = new ActionQueue();

        public static ActionQueue Singleton()
        {
            return singleton;
        }

        public void Submit(Action a) 
        {
            mu.WaitOne();
            q.Enqueue(a);
            mu.ReleaseMutex();
        }

        public Action[] CheckOut() 
        {
            mu.WaitOne();
            var res = q.ToArray(); // 将q中所有元素「复制」到新数组
            q.Clear();
            mu.ReleaseMutex();
            return res;
        }
    }
}
