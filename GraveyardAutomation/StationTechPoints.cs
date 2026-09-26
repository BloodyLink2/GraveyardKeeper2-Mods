using LazyBearTechnology;
using UnityEngine;

namespace GraveyardAutomation
{
    /// <summary>
    /// Forschungspunkte wirft das Spiel über DropSystem.DropTechPoints → TechPointsSpawner, und der setzt sie immer in
    /// die Szene des Spielers. Schließt die Mod eine Station in einer anderen Szene ab (Spieler z. B. in der Kirche,
    /// Ofen draußen), legen wir die Punkte stattdessen genau so ab, wie es der Spawner selbst tut
    /// (TechPointsSpawner.SpawnTechPoint): als TechPointDropData in den Szenendaten der Station, dazu ein sichtbares
    /// TechPointDrop, falls die Szene geladen ist. Sonst erzeugt GameScene.SpawnTechPointsFromData es beim Betreten.
    /// Mit "Collect research points" landen sie gar nicht erst am Boden, sondern gehen direkt an den Spieler.
    /// </summary>
    internal static class StationTechPoints
    {
        // Etwas streuen, damit die Punkte nicht alle auf einem Fleck liegen.
        private const float Spread = 0.4f;

        /// <summary>
        /// Schreibt die Punkte so gut wie das Spiel beim Schlafen (DropSystem.CollectViewlessTechPointDrops): je Punkt ein
        /// FlyingTechPoint, der vom Spieler zur Anzeige im HUD fliegt und erst dort zählt (ist die Anzeige nicht zu sehen,
        /// sofort). Ein Sammel-Sound pro Abschluss statt pro Punkt, sonst klingt es bei mehreren Öfen nach Dauerfeuer.
        /// </summary>
        internal static void CollectToPlayer(int r, int g, int b)
        {
            PlayerController player = MainGame.PlayerController;
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;
            Fly(pos, TechPointsSpawner.Type.R, r);
            Fly(pos, TechPointsSpawner.Type.G, g);
            Fly(pos, TechPointsSpawner.Type.B, b);
            if (r + g + b > 0) LazyAudio.Play("tech_point_collect");
        }

        private static void Fly(Vector3 pos, TechPointsSpawner.Type type, int count)
        {
            string name = TechDef.FlyingReses[(int)type];
            for (int i = 0; i < count; i++) FlyingTechPoint.Drop(pos, name);
        }

        internal static bool TryDrop(WgoData station, Vector3 pos, int r, int g, int b)
        {
            GameSceneData scene = MainGame.Instance.GameSave.WorldData.GetGameSceneDataById(station.WorldId);
            if (scene == null) return false;

            Wgo view = GameScene.GetWgoViewGlobal(station.UniqueId);
            GameScene loadedScene = view != null ? view.GetComponentInParent<GameScene>() : null;
            Transform parent = loadedScene != null ? loadedScene.transform : null;

            Add(scene, parent, station.WorldId, pos, TechPointsSpawner.Type.R, r);
            Add(scene, parent, station.WorldId, pos, TechPointsSpawner.Type.G, g);
            Add(scene, parent, station.WorldId, pos, TechPointsSpawner.Type.B, b);
            return true;
        }

        private static void Add(GameSceneData scene, Transform parent, string worldId, Vector3 pos,
            TechPointsSpawner.Type type, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * Spread;
                var data = new TechPointDropData(pos + new Vector3(offset.x, 0f, offset.y), type, worldId);
                if (parent != null) TechPointDrop.Spawn(data, parent);
                scene.AddTechPointDrop(data);
            }
        }
    }
}
