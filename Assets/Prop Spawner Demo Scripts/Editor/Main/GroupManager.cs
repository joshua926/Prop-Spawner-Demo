using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PropSpawner {
    public static class GroupManager
    {
        public static GameObject GetGroupGameObject(Rules rules)
        {
            string groupName = rules.name + " Group";
            var groupGameObject = GameObject.Find(groupName);
            if (!groupGameObject)
            {
                groupGameObject = new GameObject(groupName);
            }
            return groupGameObject;
        }           
    }
}