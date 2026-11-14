# Day and night calculator

The Weather page turns a desired real-time day and a desired real-time night, in hours, into `wm_timeScale` and `wm_timeScaleNight`.

The formula is the one in Richard's Electron app, [Miscalculator](https://github.com/jackjohns0n/Day_Night_Calculator) (`main.js`, the `item:calculate` handler, lines 77–83). This project reimplements that math. It does not embed or launch the Electron app.

```text
dayScale     = 780 / (60 × dayHours)
nightScale   = (660 / (60 × nightHours)) / dayScale
wm_timeScale      = round half away from zero to 2 decimal places (dayScale)
wm_timeScaleNight = round half away from zero to 2 decimal places (nightScale)
```

The night scale is divided by the full-precision day scale, not by the already-rounded `wm_timeScale`. That is what the original does, and it changes the second decimal on some inputs. One hour of day and one hour of night is `wm_timeScale=13` and `wm_timeScaleNight=0.85`. Two hours of day and one hour of night is `6.5` and `1.69`.

Hours must be a finite number greater than zero. The original window also limited the boxes to 0.1–24; the calculation itself does not, and neither does this one. A very large day rounds `wm_timeScale` to `0`, which is what `toFixed(2)` does there.

The same ratios run backwards. With the scales already in the file:

```text
dayHours   = 13 / wm_timeScale
nightHours = 11 / (wm_timeScaleNight × wm_timeScale)
```

That shows the real-time lengths the current numbers imply. Because the CVARs are stored at two decimal places, reversing them lands close to the hours you typed, not always on the exact same value. A scale of zero cannot be reversed.

Apply writes both keys as live lines and enables them. The preview updates as you type. A bad entry is reported in that panel only.
