using TMPro;

namespace Map
{
    public class Checkpoint : MapTile
    {
        public TextMeshPro indexText;
        public int index;
        public int healthRegenerated;
        private bool _finish;
        public static int CheckpointCount;
        protected override void Start()
        {
            base.Start();
            indexText.text = index.ToString();
            _finish = index == CheckpointCount;
        }

        public override void OnRobotArrive()
        {
            base.OnRobotArrive();
            robot.GetComponent<IDamageable>().Heal(healthRegenerated);
            if (index == robot.CheckpointsCount + 1)
            {
                robot.OnCheckpointArrive(this, _finish);
            }   
        }
        
    }
}