using UnityEngine;

namespace BastionFall.Core.Shared.Util.Identity
{
    public static class NameBank
    {
        // Pack hier gerne deine eigenen Namen rein
        private static readonly string[] Names =
        {
            "Lukas", "Mia", "Noah", "Emma", "Leon", "Sophia", "Elias", "Hannah",
            "Paul", "Anna", "Felix", "Lina", "Max", "Marie", "Jonas", "Lea",
            "David", "Mila", "Ben", "Laura", "Tom", "Clara", "Luis", "Lilly"
        };

        public static string GetRandomName()
        {
            if (Names == null || Names.Length == 0) return "Player";
            var idx = Random.Range(0, Names.Length);
            return Names[idx];
        }
    }
}