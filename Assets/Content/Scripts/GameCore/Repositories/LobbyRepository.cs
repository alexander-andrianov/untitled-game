using Content.Scripts.Gamecore.Data.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Repositories
{
    public class LobbyRepository : Repository
    {
        public ReadOnlyCollection<InGameUserModel> Users
        {
            get { return users.AsReadOnly(); }
        }

        private List<InGameUserModel> users;

        public LobbyRepository()
        {
            SetUsers(new List<InGameUserModel>{
                new InGameUserModel("Hlob", null),
                new InGameUserModel("Hlab", null),
                new InGameUserModel("Hleb", null),
                new InGameUserModel("Hlib", null),
                new InGameUserModel("Hlub", null),
                new InGameUserModel("Hljb", null),
            });
        }

        public void SetUsers(List<InGameUserModel> users)
        {
            this.users = users;
        }
    }
}
