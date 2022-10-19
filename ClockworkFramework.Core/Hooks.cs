using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClockworkFramework.Core
{
    public abstract class Hooks
    {
        public abstract void GlobalCatch(Exception exception);
        public abstract void Warning(string message);
        public abstract void LibraryUpdated(string libraryName, string message);
    }
}