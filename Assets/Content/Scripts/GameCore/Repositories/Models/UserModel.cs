using UnityEngine;

namespace Repositories.Models
{
    public class UserModel
    {
        private string name;
        private Sprite avatar;

        public UserModel(string name, Sprite avatar)
        {
            Name = name;
            Avatar = avatar;
        }

        public string Name { get => name; set => name = value; }
        public Sprite Avatar { get => avatar; set => avatar = value; }
    }
}
