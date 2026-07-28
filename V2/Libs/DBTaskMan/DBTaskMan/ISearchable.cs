using System;
using System.Collections.Generic;
using System.Text;

namespace DBTaskMan
{
    public interface ISearchable
    {
        bool ContainsText(string searchText);
        List<Attachment> Attachments { get; }
        bool IsClosed { get; }
        string ObjectDescription { get; }

    }
}
