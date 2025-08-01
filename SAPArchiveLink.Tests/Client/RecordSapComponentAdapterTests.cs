using Microsoft.QualityTools.Testing.Fakes;
using TRIM.SDK;
using TRIM.SDK.Fakes;

namespace SAPArchiveLink.Tests
{
    [TestFixture]
    public class RecordSapComponentAdapterTests
    {
        private IDisposable _shimContext;

        [SetUp]
        public void SetUp()
        {
            _shimContext = ShimsContext.Create();

            ShimTrimDateTime.NowGet = () =>
            {
                var fakeShim = new ShimTrimDateTime()
                {
                    DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11)
                };
                return fakeShim.Instance;
            };
        }

        [Test]
        public void RecordSapComponentsAdapter_AddComponent()
        {
            var r = new RecordSapComponentsAdapter(new ShimRecordSapComponents());
            ShimRecordSapComponents.AllInstances.New = (instance) =>
            {
                return new ShimRecordSapComponent();
            };
            ShimRecordSapComponent.AllInstances.SetDocumentString = (s, filePath) =>
            {
                Assert.That(filePath, Is.EqualTo("C:\\test\\test.pdf"));
            };
            r.AddComponent("compId", "0047", "application/pdf", "UTF-8", "C:\\test\\test.pdf");
        }

        [Test]
        public void RecordSapComponentsAdapter_ExtractComponentById_ReturnsSapComponentModel()
        {
            var recordsAdapter = GetRecordSapComponentsAdapter();
            var componentModel = recordsAdapter.GetComponentById("Comp2");
            Assert.Multiple(() =>
            {
                Assert.That(componentModel, Is.Not.Null);
                Assert.That(componentModel?.CompId, Is.EqualTo("COMP2"));
            });
        }

        [Test]
        public void RecordSapComponentsAdapter_ExtractComponentById_ReturnsNull()
        {
            var recordsAdapter = GetRecordSapComponentsAdapter();
            var componentModel = recordsAdapter.GetComponentById("NonExistentComp");
            Assert.That(componentModel, Is.Null);
        }

        [Test]
        public void RecordSapComponentsAdapter_GetAllComponents()
        {
            var recordsAdapter = GetRecordSapComponentsAdapter();
            var components = recordsAdapter.GetAllComponents();
            Assert.Multiple(() =>
            {
                Assert.That(components, Is.Not.Null);
                Assert.That(components.Count, Is.EqualTo(3));
                Assert.That(components[0].CompId, Is.EqualTo("COMP1"));
                Assert.That(components[1].CompId, Is.EqualTo("COMP2"));
                Assert.That(components[2].CompId, Is.EqualTo("COMP3"));
            });
        }

        [Test]
        public void RecordSapComponentsAdapter_FindComponentById_ReturnsIRecordSapComponent()
        {
            var recordsAdapter = GetRecordSapComponentsAdapter();
            var recordSapComponent = recordsAdapter.FindComponentById("Comp2");
            Assert.Multiple(() =>
            {
                Assert.That(recordSapComponent, Is.Not.Null);
                Assert.That(recordSapComponent, Is.InstanceOf<IRecordSapComponent>());
                Assert.That(recordSapComponent?.ComponentId, Is.EqualTo("COMP2"));
            });
        }

        [Test]
        public void RecordSapComponentsAdapter_FindComponentById_ReturnsNull()
        {
            var recordsAdapter = GetRecordSapComponentsAdapter();
            var recordSapComponent = recordsAdapter.FindComponentById("NonExistentComp");
            Assert.That(recordSapComponent, Is.Null);
        }

        private RecordSapComponentsAdapter GetRecordSapComponentsAdapter()
        {
            var fakeComponents = new List<RecordSapComponent>
            {
                new ShimRecordSapComponent { ComponentIdGet = () => "COMP1",  ArchiveDateGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance,
                    DateModifiedGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance },
                new ShimRecordSapComponent
                {
                    ComponentIdGet = () => "COMP2",
                    ApplicationVersionGet = () => "1.0",
                    ArchiveLinkVersionGet = () => "1.0",
                    ContentTypeGet = () => "application/pdf",
                    CharacterSetGet = () => "UTF-8",
                    BytesGet = () => 100,
                    FileNameGet = () => "test.pdf",
                    ArchiveDateGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance,
                    DateModifiedGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance
                },
                new ShimRecordSapComponent { ComponentIdGet = () => "COMP3",  ArchiveDateGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance,
                    DateModifiedGet = () => new ShimTrimDateTime { DateGet = () => new DateTime(2024, 2, 2, 11, 11, 11) }.Instance }
            };
            var shimComponents = new ShimRecordSapComponents();
            shimComponents.Bind(fakeComponents);

            return new RecordSapComponentsAdapter(shimComponents.Instance);
        }

        [TearDown]
        public void TearDown()
        {
            _shimContext.Dispose();
        }
    }
}
