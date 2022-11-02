using System;
using UnityEngine;

namespace BuildManager {

    [CreateAssetMenu(fileName = nameof(ProjectValidator), menuName = "Tools/BuildManager/Validator/" + nameof(ProjectValidator), order = 1)]
    public class ProjectValidator : ValidatorBase {
        //private DailyHelper dailyHelper = new DailyHelper();
        private bool isValid;

        public override bool Validate() {
            return true;
            //throw new NotImplementedException();
            

            //isValid = true;

            //// check if render data is missing and flag as invalid
            //if (dailyHelper.FindAllPrefabsWithMissingRenderData(true)) {
            //    isValid = false;
            //}

            //return isValid;
        }
    }
}