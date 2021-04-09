using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Cards
{
    public enum CommandType{Move3, Move2, Move1, Backup, Right, Left, Uturn}
    [CreateAssetMenu(menuName = "Command", fileName = "Command", order = 1)]
    public class Command : ScriptableObject
    {
        public static Random Random;
        [FormerlySerializedAs("Thumbnail")] public Sprite thumbnail;
        public CommandType type;
        [FormerlySerializedAs("MinWeight")] public int minWeight;
        [FormerlySerializedAs("MaxWeight")] public int maxWeight;
        [FormerlySerializedAs("Weight")] [HideInInspector]
        public int weight;
        [FormerlySerializedAs("Count")] public int count;
        public void GetWeight(){
            weight = Random.Next(minWeight, maxWeight);
        }
        public static Command FromCardInfo(GameController.CardInfo info){
            Command c = (Command)Command.CreateInstance(typeof(Command));
            c.weight = info.Weight;
            c.type = info.Type;
            c.thumbnail = GameController.instance.commandTemplates[(int)c.type].thumbnail;
            return c;
        }
        public GameController.CardInfo ToCardInfo(){
            return new GameController.CardInfo(){ Weight = weight, Type = type};
        }

        public override string ToString()
        {
            return $"{type}: {weight}";
        }
    }
}