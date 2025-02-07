using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace BoatAttack.UI
{
    public class RaceStatsPlayer : MonoBehaviour
    {

        public TextMeshProUGUI place;
        public TextMeshProUGUI playerName;
        public TextMeshProUGUI boatType;
        public TextMeshProUGUI bestLap;
        public TextMeshProUGUI time;
        private Boat _boat;
        private int _place = -1;
        private bool _update = true;

        public void Setup(Boat boat)
        {
            _boat = boat;
            playerName.text = _boat.name;
            boatType.text = boat.displayName; // TODO - need to implement
            place.text = RaceUI.OrdinalNumber(boat.Place);
        }

        private void Updatex()
        {
            if (!_update) return;
            UpdateStats();
        }

        private void LateUpdatex()
        {
            _update = !_boat.MatchComplete;
        }

        public void UpdateStats()
        {
            _place = _boat.Place;
            transform.SetSiblingIndex(_place + 1);
            place.text = RaceUI.OrdinalNumber(_boat.Place);

            var bestLapTime = RaceUI.BestLapFromSplitTimes(_boat.SplitTimes);
            bestLap.text = bestLapTime > 0 ? RaceUI.FormatRaceTime(bestLapTime) : "N/A";

            var totalTime = _boat.MatchComplete ? _boat.SplitTimes.Last() : RaceManager.RaceTime;
            time.text = RaceUI.FormatRaceTime(totalTime);
        }
    }
}
