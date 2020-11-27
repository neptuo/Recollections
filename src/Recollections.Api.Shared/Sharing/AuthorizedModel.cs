using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing
{
    public class AuthorizedModel<T>
    {
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }

        public Permission UserPermission { get; set; }

        public T Model { get; set; }

        public AuthorizedModel()
        { }

        public AuthorizedModel(T model)
        {
            Ensure.NotNull(model, "model");
            Model = model;
        }

        public void Deconstruct(out T model, out OwnerModel owner, out Permission userPermission)
        {
            model = Model;
            owner = new OwnerModel(OwnerId, OwnerName);
            userPermission = UserPermission;
        }
    }
}
