using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBProjectPal
{
    public interface ITaskOrProject
    {
        string ObjectDescription { get; }
        DateTime? ExpectedEndDate { get; }

        void AddPostDependency(ITaskOrProject postItem);
        void AddPreDependency(ITaskOrProject preItem); 
        void RemovePostDependency(ITaskOrProject postItem);
        void RemovePreDependency(ITaskOrProject preItem);

        bool IsActive { get; }

        List<ITaskOrProject> PostDependencies { get; }
    }
}
