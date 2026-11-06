# Conductor calibration

> 中文说明：本文件用于指挥玩法的节拍校准与参数调整。英文参数名和 Unity Inspector 保持一致，便于对照查找。

Open `Assets/Project/Config/ConductorTuning.asset` in the Inspector. The `PlanetPulse`
object in `ToonStudy.unity` references it; the other conductor scripts discover it
from `FirstBeatTimeline`. Tune the asset in Edit Mode so the changes persist.

中文：在 Inspector 中打开 `Assets/Project/Config/ConductorTuning.asset`。场景 `ToonStudy.unity` 中的 `PlanetPulse` 引用它，其他指挥脚本通过 `FirstBeatTimeline` 找到它。请在非运行状态（Edit Mode）调参，以保证修改持久保存。

## Mouse beat actions

### 任意方向鼠标动作

The beat action now detects a brief burst of mouse movement in **any direction**.
`Beat Motion Trigger Speed` (260 px/s) and `Beat Motion Minimum Pixels` (18 px)
define a deliberate action. `Motion Smoothing` (12) filters noisy speed;
`Beat Motion Reset Speed` (95 px/s) and `Beat Motion Cooldown Seconds` (0.38)
require a slowdown before another action can score. `Motion Threshold Pixels`
(1.5 per frame) only gives faint Motion feedback, not a beat rating. These
settings live in `ConductorTuning.asset` and apply to all fragment puzzles.
Mouse movement is sampled every rendered frame. The 0.12-second Motion feedback
cooldown and 0.45-second Motion award interval are not sampling intervals.
A scored action re-arms after the 0.38-second cooldown and either a clear
slowdown/pause or a change of direction; continuous one-way movement is one action.

中文：现在沿任意方向短促移动鼠标都可触发节拍动作，不再要求向下。`Beat Motion Trigger Speed`（260 像素/秒）与 `Beat Motion Minimum Pixels`（18 像素）控制有效动作的速度和行程；`Motion Smoothing`（12）平滑速度；`Beat Motion Reset Speed`（95 像素/秒）和 `Beat Motion Cooldown Seconds`（0.38 秒）要求动作回落后才能再次触发。`Motion Threshold Pixels`（每帧 1.5 像素）只给微弱 Motion 反馈，不能单独获得 Good/Great/Perfect。全部参数在 `ConductorTuning.asset` 中，所有残章关卡共用。横向移动也可命中，但每个第一拍只计一次。

中文：鼠标位移每个渲染帧读取一次；0.12 秒是微弱 Motion 反馈限频，0.45 秒是 Motion 积分限频，**都不是采样间隔**。一次高分动作后还需等待 0.38 秒，并明显减速／停顿或转向，才能判下一次；持续朝同一方向移动只算一次动作。

The current recording markers are **unverified**. The 110 BPM preview guides
the meteor and scoring together, but it is not guaranteed to match the
recording's actual first beats. Mark and verify the recording before judging
audio synchronization by ear.

中文：当前录音拍点仍标记为**未验证**。场景里使用 110 BPM 预览节拍，流星与评分彼此同步，却不保证与 MP3 中实际第一拍一致。若听感与流星不合，应先人工标记并核验录音拍点。

## Shared clock

### 统一时钟

`Music Mode = Timeline` is the synchronous default. The recording is scheduled
once from the transport's DSP start, and the meteor cue, first-beat judgement and
staircase all use the same clock. Starting the game from the main menu starts that
transport; pausing freezes the clock and the recording. The music plays continuously
at its original speed. It does **not** slow or stop when the baton is still.

中文：默认 `Music Mode = Timeline`。录音从统一的 DSP 时间轴定时启动，流星预告、第一拍判定和楼梯命中都参考该时间轴；从主菜单开始游戏才启动，暂停时同步冻结。录音按原速连续运行，**不挥指挥棒也不会停播**；但 0 分时音量为零，所以暂时听不到。

`Music Mode = GestureDriven` restores the earlier stop/start and pitch response.
It is an experiment, not a synced rhythm mode: changing pitch or pausing the
recording makes fixed markers drift away from the actual audio. Use Timeline for
rhythm gameplay.

中文：`Music Mode = GestureDriven` 保留之前的手势控制播放／变速实验模式。变速和暂停会让固定拍点与实际录音漂移，因此需要音乐、流星与判定同步的节奏关卡应使用 `Timeline`。

The supplied `Symphony No. 7 (Mahler).mp3` is 1303.56 seconds long. Its title
does not identify a single BPM, meter, movement, or performance tempo. The asset
therefore defaults to a clearly unverified 60 quarter notes/minute, four beats/bar
preview (a first beat every four seconds). Do not mark that preview as aligned.

中文：当前 MP3 长约 1303.56 秒。文件名无法确定整段录音的固定 BPM、拍号、乐章或演奏速度。默认 **60 BPM、每小节 4 拍**只是预览（每 4 秒一次第一拍），**未与录音校准**。

## Mark real first beats

### 标记录音中的真实第一拍

1. Open `Pulse > First Beat Marker`, enter Play Mode, start the game from the menu,
   and select the scene timeline if needed.
2. Listen and click `Mark First Beat` (or press Space with the window focused) on
   each **bar-one**. The shown times are seconds from the MP3 start. `Undo Last`
   removes an accidental tap. Only mark the section used by the Demo, or mark
   the whole recording if the Demo lasts that long.
3. Save markers to the tuning asset, listen again, correct individual
   `First Beat Seconds` values in the Inspector, then enable
   `Recording Beat Markers Verified`. An empty, non-increasing or out-of-clip
   array is rejected automatically and reverts to the preview.
4. Set `Meteor Cue Lead Seconds` for anticipation, `Meteor Strike Seconds` for
   the sweep duration, and `Judgement Offset Seconds` for a small input/audio
   latency compensation. Positive offset moves the scoring center later;
   it does not move the visible meteor or the recording.

中文操作步骤：

1. 打开 Unity 菜单 `Pulse > First Beat Marker`，进入 Play Mode 并从主菜单开始游戏；必要时在窗口中选择场景时间轴。
2. 边听边在**每小节第一拍**点击 `Mark First Beat`，或在该窗口获得焦点时按空格。显示时间从 MP3 开头计算；误记可以点 `Undo Last`。Demo 可以只标记实际使用的段落。
3. 保存到 `ConductorTuning.asset`，复听并在 Inspector 的 `First Beat Seconds` 中逐点修正；确认后才启用 `Recording Beat Markers Verified`。空数组、非递增或超出录音长度的标记无法通过验证，系统会继续使用预览节奏。
4. `Meteor Cue Lead Seconds` 控制流星提前蓄亮的时间，`Meteor Strike Seconds` 控制划过时长。`Judgement Offset Seconds` 补偿少量输入／听感延迟：正值使评分中心延后，**不会**移动流星或录音。

`Meteor Beat Phase` selects where the judged first beat falls within the
strike animation. At 0.5, the meteor starts half a strike duration before
the beat and passes its midpoint at the beat; its endpoints and duration
remain unchanged. `Input Latency Compensation Seconds` accounts for mouse
filtering and reaction to the visual cue: positive values score the detected
downstroke earlier. Adjust it while watching the debug ratings, then refine
the Perfect/Great windows rather than widening Good indiscriminately.

中文：`Meteor Beat Phase` 指定第一拍在流星划过动画中的位置。默认 0.5 表示流星提前半个动画时长开始，在拍点经过轨迹中段；起点、终点和总时长不变。`Input Latency Compensation Seconds` 默认 0.08 秒，用来补偿鼠标速度平滑及看到提示后挥动的延迟：正值把检测到的下挥往前校正。可结合 `T` 调试面板观察评级，再微调 Perfect/Great 窗口，不要盲目扩大 Good 窗口。

The marker tool saves an *unverified* asset deliberately. In Play Mode Unity may
restore serialized scene state on exit; verify the asset after stopping.

中文：工具保存拍点时会故意保留“未验证”状态。退出 Play Mode 后检查拍点是否已保存，再手动启用 Verified。

## Response mapping

### 评分与反馈映射

`Perfect / Great / Good Window Seconds` define early/late ratings around a
marked first beat. `Max Assist Window Seconds` expands the Good window after
`Idle Grace Seconds` without widening Perfect/Great. An unmatched downstroke
has no separate rating or points. Small movement yields Motion points.

Current scene values are Perfect +/-0.12 s, Great +/-0.24 s, Good +/-0.42 s,
and assisted Good up to +/-0.55 s. A first beat scores only once; a movement
too early in the bar must not consume the visible meteor's judged beat.

中文：`Perfect / Great / Good Window Seconds` 是围绕第一拍的提前／延后容差。经过 `Idle Grace Seconds` 仍没有进展时，Good 的容差可扩大到 `Max Assist Window Seconds`，Perfect 和 Great 的容差不变。未命中的下挥没有独立评级或积分；轻微鼠标动作按 `Motion` 积分。

中文：当前配置为 Perfect 前后 0.12 秒、Great 前后 0.24 秒、Good 前后 0.42 秒；长时间未进展时 Good 最多扩至前后 0.55 秒。每个第一拍只给一次评级；明显提前的动作不应抢掉流星抵达拍点时的评分机会。

`Motion Score Cap` limits the active contribution of small mouse movements
without limiting Good/Great/Perfect. `Motion Award Interval Seconds` limits
how often those small movements can award points. The current settings are
5 points per Motion, at most 10 active Motion points, once per 0.45 seconds.
This keeps idle fidgeting at no more than 10% of full light and volume.

中文：`Motion Score Cap` 只限制轻微鼠标动作的**当前有效积分**，不限制 Good/Great/Perfect。`Motion Award Interval Seconds` 限制微动作加分频率。当前配置为每次 5 分、Motion 最多同时保留 10 分、两次加分至少间隔 0.45 秒。因此单靠轻微晃动最多达到满分亮度和音量的 10%；若仍觉得太亮，可继续降低 `Motion Score Cap` 或 `Motion Points`。

Score starts at 0 and is not capped internally. `Motion / Good / Great /
Perfect Points` add 5 / 10 / 20 / 50 by default. Each award has its own
expiry: `Motion / Good / Great / Perfect Duration Seconds` default to
3 / 4 / 4.5 / 5 seconds. When an award expires, only its points are removed.
`Maximum Light Score` (default 100) caps the *light and volume mapping*,
not stored score: e.g. 150 and 100 look and sound the same, but when 50
points expire, the remaining score is still 100. Zero maps to
`Initial Darkness` (1 = black) and no gesture light; 100 or above maps to
`Full Score Darkness` (0 = no blackout) and `Full Score Brightness`.
`Brighten Seconds` and `Brighten Curve` smooth both rising and falling light.
Pausing the menu freezes award expiry. Missed beats do not immediately
subtract points, and persistent path progress is separate from this score.

中文：积分从 0 开始，**内部不封顶**。`Motion / Good / Great / Perfect Points` 默认分别加 5 / 10 / 20 / 50 分；每笔积分独立计时，`Motion / Good / Great / Perfect Duration Seconds` 默认是 3 / 4 / 4.5 / 5 秒，到期只扣除该笔分数。`Maximum Light Score`（默认 100）仅是**光照与音量的映射上限**：例如内部有 150 分与 100 分时显示／听感相同，但其中 50 分过期后仍剩 100 分。0 分对应 `Initial Darkness`（1 为全暗）且没有手势灯光；100 分及以上对应 `Full Score Darkness`（0 为不遮暗）和 `Full Score Brightness`。`Brighten Seconds`、`Brighten Curve` 同时平滑亮起与回暗。菜单暂停时积分计时冻结；漏拍不会立即扣分。已解锁的光路范围与谜题形态保留，但其发光和点光会随积分回落；台阶升起后不会缩回。

`Light Score Curve` maps score to visual light before the blackout and local
lights are applied. The current curve maps 0/60/100 points to 0%/28%/100%
light, creating a clearer difference between medium and full score. It does
not change stored points or music volume.

中文：`Light Score Curve` 只改变积分到视觉光照的映射；当前 0/60/100 分分别对应 0%/28%/100% 的光照强度，使中段和满分有明显差距。它不改变实际积分或音乐音量。曲线可直接在配置资产中编辑。

In Timeline mode, the same timed score controls recording volume:
0 points = silence; `Maximum Light Score` (default 100) or above = `Recording Volume`.
Motion / Good / Great / Perfect add the same 5 / 10 / 20 / 50 points to
both light and music; there is no separate music score. `Score Volume
Transition Seconds` and `Score Volume Curve` smooth both increases and
decreases from the current volume to its new score target. Playback remains at original speed
and continues silently at zero score so first-beat markers stay aligned.
The `Music Fade In/Out Seconds` fields belong to experimental GestureDriven
mode, not this score-controlled Timeline volume.

中文：`Timeline` 模式下，**同一份有时效的积分**同时控制光照和录音音量，不另设音乐分数。0 分静音，100 分及以上达到 `Recording Volume`；每笔分数到期后，若总分降到 100 以下，音量会随之降低。`Score Volume Transition Seconds` 和 `Score Volume Curve` 决定音量升降的柔和过渡。录音始终原速向前播放，0 分只是静音而非暂停，以免与第一拍标记失去同步。`Music Fade In/Out Seconds` 属于实验性 `GestureDriven` 模式，不控制这里的积分音量。

`Glow Per Light Strength` and `Cluster Light Per Strength` set plant emission
and its point-light response to the same rating strength. The two `...Visibility`
settings add plant light in proportion to current score visibility. The
persistent path geometry still advances on first-beat hits, but does not
independently change the global score-driven brightness.

中文：`Glow Per Light Strength` 控制植物自发光随积分光强增加的程度；`Cluster Light Per Strength` 控制植物附近点光源。名称包含 `Visibility` 的两项随当前积分对应的可见度补充植物光。第一拍命中会永久扩展可照亮的光路范围，但当前亮度仍由积分决定；残章谜题的模型、点光同样如此。

`Staircase Trigger Distance`, `Staircase Minimum Rating`, and
`Staircase Required Hits` gate the existing `Raise` trigger. The hits must be
consecutive *first beats* while in range; missed beats reset this streak but
never remove persistent path progress. The defaults are Good, 3 hits, 50 units.
Raising the staircase completes only that puzzle. First-beat ratings, temporary
light and music scores, and the remaining path progress continue afterward.

中文：`Staircase Trigger Distance`、`Staircase Minimum Rating`、`Staircase Required Hits` 分别是与楼梯的距离、最低评分和所需连续命中次数。只计范围内**连续小节第一拍**；漏拍会清空这一轮连续命中，但不会移除已经点亮的持久光路。默认是 50 单位内、Good 及以上、连续 3 次。

中文：台阶升起只代表该谜题完成，不会停止之后的第一拍评分；后续指挥仍能累积分数，控制环境光、音乐音量并推进剩余光路。

## Journal fragment puzzles

### 日记残章关卡判定

`Fragment Puzzles` in `ConductorTuning.asset` has one entry for each of
chapters 2-6. `Chapter` selects the matching scene site. Set `Mode`,
`Require Beat Puzzle`, `Interaction Distance`, `Required Hits`, `Minimum
Rating`, and `Anchor Radius` independently per chapter. The scene object
still owns its visual references and ordered anchor Transforms. `VisitAnchors`
requires one valid hit at each anchor in sequence; keep `Required Hits`
equal to the anchor count. The first chapter's staircase uses its separate
`Staircase` section above.

中文：在 `ConductorTuning.asset > Journal Fragment Puzzles` 中分别配置第 2 至第 6 章。`Chapter` 对应日记章节号；`Mode` 选择累计、连续、左右交替或依次到达锚点；`Require Beat Puzzle` 控制是否需要解谜；`Interaction Distance` 是玩家与关卡中心的判定距离；`Required Hits` 是所需有效命中次数；`Minimum Rating` 是最低评分；`Anchor Radius` 是依次拜访模式下每个空间锚点的半径。场景组件仍负责残章模型、美术反馈和有序锚点 `Transform` 引用。`VisitAnchors` 模式请让命中次数与锚点数量相同；第一章的台阶判定继续在上方 `Staircase` 区域调整。

| Chapter | Mode | Distance | Hits | Minimum rating | Anchors |
| --- | --- | ---: | ---: | --- | ---: |
| 2 | Consecutive | 5 | 3 | Good | 0 |
| 3 | AlternateSides | 7 | 2 | Good | 0 |
| 4 | VisitAnchors | 7.5 | 3 | Good | 3 |
| 5 | Accumulate | 5 | 4 | Good | 0 |
| 6 | VisitAnchors | 7.5 | 4 | Good | 4 |

中文：以上数值迁自当前场景。残章关卡的 `Perfect / Great / Good` 时间窗口与第一章共用上方 `Judgement` 配置，不再另写固定窗口。旧场景中第 2、3、5 章的最低评分存储为已移除的枚举值 `1`，现统一校正为 `Good`。

The original serialized fields remain in scene components for compatibility,
but when a tuning asset is assigned the fields described above are read from
that asset. `DownstrokeInput` keeps the raw mouse filtering, trigger speed,
cooldown and stroke-distance controls in its component Inspector.

中文：场景组件中的旧字段仍为兼容保留；引用了调参资产时，以上参数以 `ConductorTuning.asset` 中的值为准。鼠标原始检测的平滑、触发速度、冷却和最短下挥行程仍在 `DownstrokeInput` 组件的 Inspector 中调整。
