using TMPro;
using UnityEngine;

namespace Map
{
    public class Checkpoint : MapTile
    {
        public TextMeshPro indexText;
        public int index;
        public int healthRegenerated;
        private bool _finish;
        public Texture2D checkedTexture;
        public static int CheckpointCount;
        protected override void Start()
        {
            base.Start();
            indexText.text = index.ToString();
            _finish = index == CheckpointCount - 1;
            if (_finish) indexText.color = Color.red;
        }

        public override void OnRobotArrive()
        {
            base.OnRobotArrive();
            robot.GetComponent<IDamageable>().Heal(healthRegenerated);
            if (index != robot.CheckpointsCount + 1 || !robot.hasAuthority) return;
            GetComponent<MeshRenderer>().material.mainTexture = checkedTexture;
            robot.OnCheckpointArrive(this, _finish);
        }

        public override void ShowTilePropertiesUi()
        {
            base.ShowTilePropertiesUi();
            ShowProperty(this, "index").OnChangeValue += value => indexText.text = value.ToString();
            ShowProperty(this, "healthRegenerated");
        }
    }
}