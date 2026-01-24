using Accord.Imaging;
using Accord.Imaging.Filters;
using BenchmarkDotNet.Attributes;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Image.FileFormat.FITS;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Benchmarks {
    [MemoryDiagnoser]
    [DisassemblyDiagnoser(maxDepth: 2)]
    public class DebayerAlgorithms {
        private ImageDataFactory dataFactory;
        private IImageData data;
        private IRenderedImage render;
        private SensorType bayerPattern = SensorType.BGGR;

        public string Path { get; } = @"E:\2020-04-22_22-57-18__-20.70°C_120.00s_RMS0.85_00101.fit";

        [GlobalSetup]
        public async Task Setup() {
            var profileMock = new Mock<IProfileService>();
            string outValue = "";
            profileMock.Setup(m => m.ActiveProfile.PluginSettings.TryGetValue(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    out outValue))
                .Returns(true);
            profileMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInnerCropRatio).Returns(1);
            profileMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusOuterCropRatio).Returns(1);
            profileMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusUseBrightestStars).Returns(0);
            profileMock.SetupGet(x => x.ActiveProfile.ImageSettings.AnnotateUnlimitedStars).Returns(false);
            dataFactory = new ImageDataFactory(profileMock.Object, new PluggableBehaviorSelector<IStarDetection, StarDetection>(new StarDetection()), new PluggableBehaviorSelector<IStarAnnotator, StarAnnotator>(new StarAnnotator()));


            var imageDataFactory = new ImageDataFactory(default, default, default);
            data = await FITS.Load(new Uri(Path), false, dataFactory, default);
            render = data.RenderImage();            

        }

        [Benchmark(Baseline = true)]
        public async Task CurrentBayerFilter16bpp() {
            var filter = new BayerFilter16bpp();
            filter.SaveColorChannels = false;
            filter.SaveLumChannel = false;


            switch (bayerPattern) {
                case SensorType.RGGB:
                    filter.BayerPattern = new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } };
                    break;

                case SensorType.RGBG:
                    filter.BayerPattern = new int[,] { { RGB.G, RGB.B }, { RGB.G, RGB.R } };
                    break;

                case SensorType.GRGB:
                    filter.BayerPattern = new int[,] { { RGB.B, RGB.G }, { RGB.R, RGB.G } };
                    break;

                case SensorType.GRBG:
                    filter.BayerPattern = new int[,] { { RGB.G, RGB.B }, { RGB.R, RGB.G } };
                    break;

                case SensorType.GBGR:
                    filter.BayerPattern = new int[,] { { RGB.R, RGB.G }, { RGB.B, RGB.G } };
                    break;

                case SensorType.GBRG:
                    filter.BayerPattern = new int[,] { { RGB.G, RGB.R }, { RGB.B, RGB.G } };
                    break;

                case SensorType.BGRG:
                    filter.BayerPattern = new int[,] { { RGB.G, RGB.R }, { RGB.G, RGB.B } };
                    break;

                case SensorType.BGGR:
                    filter.BayerPattern = new int[,] { { RGB.R, RGB.G }, { RGB.G, RGB.B } };
                    break;

                default:
                    throw new InvalidImagePropertiesException(string.Format(Loc.Instance["LblUnsupportedCfaPattern"], bayerPattern));
            }
            using var bmp = ImageUtility.BitmapFromSource(render.OriginalImage, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
            filter.Apply(bmp);
        }

        [Benchmark()]
        public async Task NewBayerFilter16bpp() {
            var filter = new BayerFilter16bppNew();
            filter.SaveColorChannels = false;
            filter.SaveLumChannel = false;


            switch (bayerPattern) {
                case SensorType.RGGB:
                    filter.BayerPattern = new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } };
                    break;

                case SensorType.RGBG:
                    filter.BayerPattern = new int[,] { { RGB.G, RGB.B }, { RGB.G, RGB.R } };
                    break;

                case SensorType.GRGB:
                    filter.BayerPattern = new int[,] { { RGB.B, RGB.G }, { RGB.R, RGB.G } };
                    break;

                case SensorType.GRBG:
                    filter.BayerPattern = new int[,] { { RGB.G, RGB.B }, { RGB.R, RGB.G } };
                    break;

                case SensorType.GBGR:
                    filter.BayerPattern = new int[,] { { RGB.R, RGB.G }, { RGB.B, RGB.G } };
                    break;

                case SensorType.GBRG:
                    filter.BayerPattern = new int[,] { { RGB.G, RGB.R }, { RGB.B, RGB.G } };
                    break;

                case SensorType.BGRG:
                    filter.BayerPattern = new int[,] { { RGB.G, RGB.R }, { RGB.G, RGB.B } };
                    break;

                case SensorType.BGGR:
                    filter.BayerPattern = new int[,] { { RGB.R, RGB.G }, { RGB.G, RGB.B } };
                    break;

                default:
                    throw new InvalidImagePropertiesException(string.Format(Loc.Instance["LblUnsupportedCfaPattern"], bayerPattern));
            }
            using var bmp = ImageUtility.BitmapFromSource(render.OriginalImage, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
            filter.Apply(bmp);
        }
    }

    public class PluggableBehaviorSelector<T, DefaultT> : BaseINPC, IPluggableBehaviorSelector<T>
        where T : class, IPluggableBehavior
        where DefaultT : T {
        private readonly DefaultT ninaDefault;
        private string selectedContentId;

        public PluggableBehaviorSelector(DefaultT ninaDefault) {
            this.ninaDefault = ninaDefault;
            Behaviors = new AsyncObservableCollection<T>();
            Behaviors.Add(ninaDefault);
        }

        private void DetectSelectedBehaviorChanged() {
            SelectedBehaviorChanged?.Invoke(this, new EventArgs());
            selectedContentId = "";
        }

        public Type GetInterfaceType() {
            return typeof(T);
        }

        private void Behaviors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            RaisePropertyChanged(nameof(Behaviors));
            RaisePropertyChanged(nameof(SelectedBehavior));
        }

        private AsyncObservableCollection<T> behaviors;

        public event EventHandler SelectedBehaviorChanged;

        public AsyncObservableCollection<T> Behaviors {
            get => behaviors;
            set {
                if (behaviors != value) {
                    if (behaviors != null) {
                        behaviors.CollectionChanged -= Behaviors_CollectionChanged;
                    }
                    behaviors = value;
                    if (behaviors != null) {
                        behaviors.CollectionChanged += Behaviors_CollectionChanged;
                    }
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SelectedBehavior));
                }
            }
        }

        public T SelectedBehavior {
            get => GetBehavior();
            set {
                if (value == null) {
                    throw new ArgumentException("SelectedBehavior cannot be set to null", "SelectedBehavior");
                }
                if (!Behaviors.Any(b => b.ContentId == value.ContentId)) {
                    throw new ArgumentException($"{value.ContentId} is not a plugged {typeof(T).FullName} behavior", "SelectedBehavior");
                }
            }
        }

        public T GetBehavior(string pluggableBehaviorContentId) {
            if (String.IsNullOrEmpty(pluggableBehaviorContentId)) {
                return ninaDefault;
            }

            var selected = behaviors.FirstOrDefault(b => b.ContentId == pluggableBehaviorContentId);
            if (selected != null) {
                return selected;
            }
            return ninaDefault;
        }

        public T GetBehavior() {
            return GetBehavior(null);
        }

        public void AddBehavior(object behavior) {
            var typedBehavior = behavior as T;
            if (behavior == null) {
                throw new ArgumentException($"Can't add behavior {behavior.GetType().FullName} since it doesn't implement {typeof(T).FullName}");
            }

            Behaviors.Add(typedBehavior);
        }
    }
}
