# Exact code change intervals
Base: `bb3e5591908d2525f544d7e8968f84f6f248ec1b`. New source ref: `perf/async-pipeline-hardening`.
Intervals are generated from `git diff --unified=0`; intervening lines are unchanged. An insertion has an old-side anchor, and a deletion has a new-side anchor. All nine changed C# files are included.
## `osu.Game.Benchmarks/BenchmarkTaskChain.cs`
| Original lines | New lines |
| --- | --- |
| new file | [1–38](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game.Benchmarks/BenchmarkTaskChain.cs#L1-L38) |

## `osu.Game.Tests/Beatmaps/LocalCachedBeatmapMetadataSourceTest.cs`
| Original lines | New lines |
| --- | --- |
| new file | [1–500](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game.Tests/Beatmaps/LocalCachedBeatmapMetadataSourceTest.cs#L1-L500) |

## `osu.Game.Tests/Database/OnlineLookupCacheTest.cs`
| Original lines | New lines |
| --- | --- |
| new file | [1–217](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game.Tests/Database/OnlineLookupCacheTest.cs#L1-L217) |

## `osu.Game.Tests/Database/TestSceneBeatmapImporterCancellation.cs`
| Original lines | New lines |
| --- | --- |
| new file | [1–160](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game.Tests/Database/TestSceneBeatmapImporterCancellation.cs#L1-L160) |

## `osu.Game.Tests/NonVisual/TaskExtensionsTest.cs`
| Original lines | New lines |
| --- | --- |
| new file | [1–222](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game.Tests/NonVisual/TaskExtensionsTest.cs#L1-L222) |

## `osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs`
| Original lines | New lines |
| --- | --- |
| after [4](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L4) | [5](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L5-L5) |
| after [7](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L7) | [9](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L9-L9) |
| [29](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L29-L29) | [31–37](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L31-L37) |
| after [33](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L33) | [42–52](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L42-L52) |
| [47–49](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L47-L49) | [66–69](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L66-L69) |
| [52](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L52-L52) | [72](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L72-L72) |
| [54–58](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L54-L58) | [74](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L74-L74) |
| [65](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L65-L65) | [81](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L81-L81) |
| [76–80](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L76-L80) | [92–98](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L92-L98) |
| after [82](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L82) | [101–115](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L101-L115) |
| [86](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L86-L86) | [119](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L119-L119) |
| [101](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L101-L101) | [134](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L134-L134) |
| [129–134](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L129-L134) | [162–164](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L162-L164) |
| [149–157](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L149-L157) | [179–180](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L179-L180) |
| [159–166](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L159-L166) | [182–185](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L182-L185) |
| [171](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L171-L171) | [190–194](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L190-L194) |
| [173–175](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L173-L175) | [196](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L196-L196) |
| [178](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L178-L178) | [199–202](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L199-L202) |
| [180–181](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L180-L181) | [204–207](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L204-L207) |
| [183](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L183-L183) | [209–211](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L209-L211) |
| [185–187](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L185-L187) | [213–216](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L213-L216) |
| [189–194](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L189-L194) | [218–220](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L218-L220) |
| [196–197](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L196-L197) | [222–225](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L222-L225) |
| [199–214](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L199-L214) | [227](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L227-L227) |
| [216–218](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L216-L218) | [229](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L229-L229) |
| [220–224](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L220-L224) | [231](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L231-L231) |
| [226–228](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L226-L228) | [233–240](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L233-L240) |
| [232](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L232-L232) | [244](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L244-L244) |
| [234](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L234-L234) | after 245 |
| [236](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L236-L236) | [247–289](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L247-L289) |
| [238–242](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L238-L242) | [291–296](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L291-L296) |
| [244](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L244-L244) | after 297 |
| [246](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L246-L246) | [299–311](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L299-L311) |
| after [250](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L250) | [316–318](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L316-L318) |
| [253](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L253-L253) | [321–333](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L321-L333) |
| [266](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L266-L266) | [346](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L346-L346) |
| [297](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L297-L297) | [377](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L377-L377) |
| [416](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L416-L416) | [496–522](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs#L496-L522) |

## `osu.Game/Database/OnlineLookupCache.cs`
| Original lines | New lines |
| --- | --- |
| [70](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L70-L70) | [70–75](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L70-L75) |
| [80](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L80-L80) | [85](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L85-L85) |
| [98–99](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L98-L99) | [103](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L103-L103) |
| [101](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L101-L101) | [105–106](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L105-L106) |
| [103–108](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L103-L108) | [108](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L108-L108) |
| [110–111](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L110-L111) | [110–114](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L110-L114) |
| [113](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L113-L113) | [116–121](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L116-L121) |
| [116](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L116-L116) | after 123 |
| [118–122](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L118-L122) | [125–126](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L125-L126) |
| [124–125](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L124-L125) | [128–129](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L128-L129) |
| [127–129](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L127-L129) | [131–133](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L131-L133) |
| [131](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L131-L131) | [135](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L135-L135) |
| [133–137](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L133-L137) | [137](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L137-L137) |
| [139](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L139-L139) | [139](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L139-L139) |
| [141–142](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L141-L142) | [141–144](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L141-L144) |
| [144](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L144-L144) | [146–147](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L146-L147) |
| [148–150](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L148-L150) | [151–155](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L151-L155) |
| [152–153](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/OnlineLookupCache.cs#L152-L153) | [157–164](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/OnlineLookupCache.cs#L157-L164) |

## `osu.Game/Database/RealmArchiveModelImporter.cs`
| Original lines | New lines |
| --- | --- |
| [121–122](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/RealmArchiveModelImporter.cs#L121-L122) | after 120 |
| after [124](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/RealmArchiveModelImporter.cs#L124) | [123–124](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/RealmArchiveModelImporter.cs#L123-L124) |
| [149](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Database/RealmArchiveModelImporter.cs#L149-L149) | [149–150](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Database/RealmArchiveModelImporter.cs#L149-L150) |

## `osu.Game/Extensions/TaskExtensions.cs`
| Original lines | New lines |
| --- | --- |
| [7](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L7-L7) | after 6 |
| [21](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L21-L21) | [20–24](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Extensions/TaskExtensions.cs#L20-L24) |
| [30–34](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L30-L34) | [33–35](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Extensions/TaskExtensions.cs#L33-L35) |
| [36](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L36-L36) | [37](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Extensions/TaskExtensions.cs#L37-L37) |
| [38](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L38-L38) | [39](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Extensions/TaskExtensions.cs#L39-L39) |
| [40](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L40-L40) | [41](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Extensions/TaskExtensions.cs#L41-L41) |
| [42](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L42-L42) | [43](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Extensions/TaskExtensions.cs#L43-L43) |
| [44–58](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L44-L58) | [45–46](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Extensions/TaskExtensions.cs#L45-L46) |
| [60–65](https://github.com/ppy/osu/blob/bb3e5591908d2525f544d7e8968f84f6f248ec1b/osu.Game/Extensions/TaskExtensions.cs#L60-L65) | [48](https://github.com/asdkabachok/osu/blob/perf/async-pipeline-hardening/osu.Game/Extensions/TaskExtensions.cs#L48-L48) |

