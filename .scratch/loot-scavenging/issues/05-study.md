# 05: Study at a research bench

**What to build:** A researcher carries scrap to a research bench and uses it up for research points, as a job in the Research work type. Points go to the project currently being researched if it is of the scrap's era or lower, through the game's research manager, and study can finish a project. When nothing is eligible the job waits and the scrap stays in storage. Settings cover which eras can be studied, recycle or study, and techprint projects. The scrap's inspect text shows the points per unit and whether it can be studied now. See the spec's "Study" section and the requirements learned from Technology Blueprints.

**Blocked by:** 03

**Status:** ready-for-agent

**Recommended model:** `opus` — A custom job and work giver, research-factor and techprint edge cases, and the spec's lessons from Technology Blueprints.

- [ ] Studying same-era scrap advances the current project, and finishing a study shows the gain and the project.
- [ ] Higher-era scrap is refused, and the inspect text says why.
- [ ] With no current project the job does not start.
- [ ] A storyteller research factor of zero, and one above 100%, write no invalid value into project progress.
- [ ] With the techprint setting off, projects that need techprints receive nothing.
- [ ] Study time scales with the points paid, research speed applies, and the researcher gains Intellectual experience.
- [ ] Both the 1.7 and 1.6 builds compile.
