# Day and night calculator

The Weather page edits `wm_timeScale` and `wm_timeScaleNight` in one panel. They are not listed again as separate rows.

Enter how long you want the day and the night to last, in real-world hours. The panel calculates `wm_timeScale` (day speed) and `wm_timeScaleNight` (night speed relative to the day). Change the hours, or edit a scale in the panel. Either side updates the other. Those boxes are a pending preview. **Apply** writes the two scales into the file, and only when both sides are valid. An empty or invalid box does not change the file. Each scale has its own **Use this value** checkbox, and that checkbox does take effect immediately.

`wm_timeScale` is how fast the day runs. A larger number makes the day pass in fewer real hours. `wm_timeScaleNight` is how fast the night runs compared with the day. The night's speed against real time is the two numbers multiplied.

The formula treats daytime as 13 in-game hours (780 minutes) and nighttime as 11 in-game hours (660 minutes). Divide 13 by the real hours you want the day to last, and that is `wm_timeScale`. Divide 11 by the real hours you want the night to last, then divide that by the day speed, and that is `wm_timeScaleNight`. Both are rounded to two decimal places. The night value uses the full day speed, before that rounding.

The template's stock values are `wm_timeScale=2` and `wm_timeScaleNight=4`. Read backwards, that is a 6.5-hour day and a 1.375-hour night, 7.875 hours for the whole cycle. The template comment says a setting of 2 makes a full day pass in 12 real hours. That 12 hours is the whole cycle when night runs at the same speed as day (`wm_timeScaleNight=1`): 6.5 hours of day plus 5.5 hours of night. The stock night value of 4 is faster, so the night is 1.375 hours and the cycle is 7.875 hours, not 12.

The line under the hour boxes is the pending calculation. It changes on every edit, including when you clear a box. An empty or invalid box clears the scale it feeds and says what is missing, for example: Enter a night length to calculate wm_timeScaleNight. Apply stays off until both sides are valid. Until you Apply, the file is left as it was. **Saved in this file**, further down, quotes the values the file holds now and reads them back into real hours with the same math. The scales are stored to two decimal places, so the hours can sit a little off what you typed.

Clear **Use this value** and that CVAR is not written as a live setting. The server uses its stock default for it, and the saved line names the one that is turned off.

`wm_timeScaleWeather` is separate. It stays in the list below the panel and does not set the day or night length.

The math is a port of Richard's Electron app, [Miscalculator](https://github.com/jackjohns0n/Day_Night_Calculator) (`main.js`, the `item:calculate` handler, lines 77–83). This project does not embed or launch that app.

```text
dayScale     = 780 / (60 × dayHours)          // 13 / dayHours
nightScale   = (660 / (60 × nightHours)) / dayScale
wm_timeScale      = round half away from zero to 2 decimal places (dayScale)
wm_timeScaleNight = round half away from zero to 2 decimal places (nightScale)
```

The night scale is divided by the full-precision day scale, not by the already-rounded `wm_timeScale`. One hour of day and one hour of night is `wm_timeScale=13` and `wm_timeScaleNight=0.85`. Two hours of day and one hour of night is `6.5` and `1.69`.

Hours must be a finite number greater than zero. The original window also limited the boxes to 0.1–24; the calculation itself does not, and neither does this one. A very large day rounds `wm_timeScale` to `0`, which is what `toFixed(2)` does there.

The reverse of the stored scales is:

```text
dayHours   = 13 / wm_timeScale
nightHours = 11 / (wm_timeScaleNight × wm_timeScale)
```

A scale of zero cannot be reversed. A bad entry is reported in the day/night panel only. Empty, blank, and invalid text leave the saved CVARs untouched; the pending boxes go blank instead of keeping the previous result.
