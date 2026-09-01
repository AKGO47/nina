#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;

namespace NINA.Sequencer.SequenceItem.Rotator {

    /// <summary>
    /// Move the rotator to a sky position angle (north through east).
    /// Requires a prior plate-solve Sync (CenterAndRotate / SolveAndRotate).
    /// Distinct from MoveRotatorMechanical: that one takes the hardware angle,
    /// which is not the same number as InputTarget.PositionAngle after HALF-range
    /// folding and Sync offset.
    /// </summary>
    [ExportMetadata("Name", "Lbl_SequenceItem_Rotator_MoveRotator_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Rotator_MoveRotator_Description")]
    [ExportMetadata("Icon", "RotatorSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Rotator")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MoveRotator : SequenceItem, IValidatable {

        [ImportingConstructor]
        public MoveRotator(IRotatorMediator RotatorMediator) {
            this.rotatorMediator = RotatorMediator;
        }

        private MoveRotator(MoveRotator cloneMe) : this(cloneMe.rotatorMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new MoveRotator(this) {
                Position = Position
            };
        }

        private IRotatorMediator rotatorMediator;

        private float position = 0;

        [JsonProperty]
        public float Position {
            get => position;
            set {
                position = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return rotatorMediator.Move(Position, token);
        }

        public bool Validate() {
            var i = new List<string>();
            var info = rotatorMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblRotatorNotConnected"]);
            } else if (!info.Synced) {
                i.Add(Loc.Instance["LblRotatorNotSynced"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MoveRotator)}, Position: {Position}";
        }
    }
}
