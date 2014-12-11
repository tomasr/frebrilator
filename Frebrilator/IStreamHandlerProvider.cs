using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winterdom.Frebrilator {
  public interface IStreamHandlerProvider : IDisposable {
    IStreamHandler Get(Guid activityId);
  }
}
