#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Sequencer.SequenceItem.Rotator;
using NINA.Equipment.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Rotator {

    [TestFixture]
    internal class MoveRotatorTest {
        public Mock<IRotatorMediator> rotatorMediatorMock;

        [SetUp]
        public void Setup() {
            rotatorMediatorMock = new Mock<IRotatorMediator>();
        }

        [Test]
        public void Clone_ItemClonedProperly() {
            var sut = new MoveRotator(rotatorMediatorMock.Object);
            sut.Name = "SomeName";
            sut.Description = "SomeDescription";
            sut.Icon = new System.Windows.Media.GeometryGroup();
            sut.Position = 42.5f;
            var item2 = (MoveRotator)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Position.Should().Be(sut.Position);
        }

        [Test]
        public void Validate_NoIssues() {
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo() { Connected = true, Synced = true });

            var sut = new MoveRotator(rotatorMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeTrue();
            sut.Issues.Should().BeEmpty();
        }

        [Test]
        public void Validate_NotConnected_OneIssue() {
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo() { Connected = false, Synced = false });

            var sut = new MoveRotator(rotatorMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        public void Validate_NotSynced_OneIssue() {
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo() { Connected = true, Synced = false });

            var sut = new MoveRotator(rotatorMediatorMock.Object);
            var valid = sut.Validate();

            valid.Should().BeFalse();
            sut.Issues.Should().HaveCount(1);
        }

        [Test]
        [TestCase(0)]
        [TestCase(45)]
        [TestCase(180)]
        public async Task Execute_CallsSkyMove(float position) {
            rotatorMediatorMock.Setup(x => x.GetInfo()).Returns(new RotatorInfo() { Connected = true, Synced = true });

            var sut = new MoveRotator(rotatorMediatorMock.Object);
            sut.Position = position;

            var cts = new CancellationTokenSource();
            await sut.Execute(default, cts.Token);

            rotatorMediatorMock.Verify(x => x.Move(It.Is<float>(p => p == position), cts.Token), Times.Once);
            rotatorMediatorMock.Verify(x => x.MoveMechanical(It.IsAny<float>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
