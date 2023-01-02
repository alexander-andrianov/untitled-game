using Content.Scripts.Gamecore.Data.Enums;
using Repositories.Models;
using UnityEngine;

namespace Content.Scripts.Gamecore.Data.Models
{
    public class InGameUserModel : UserModel
    {
        public UserGameplayStatus UserGameplayStatus { get; set; }

        public InGameUserModel(string name, Sprite avatar) : base(name, avatar)
        {
        }
    }
}
