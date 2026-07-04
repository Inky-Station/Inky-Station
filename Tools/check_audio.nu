#!/usr/bin/env nu

const ignored = [
    Announcements
    Misc
    Ambience
    Ambient # bruh
    Weather
    Effects # this is a shitdump of everything including stuff that SHOULD be mono but idc
    CosmicCult # again, they put it all into one folder, whatever
    Shadowling # same
    Heretic
]
const max_sound_len = 6.5 # if it's longer than this it's likely just music
const extension = 'ogg'

# this better be a c# test but i'm laazy

if (which ^ffprobe | is-empty) {
    print "This requires ffprobe (ffmpeg) to run"
    exit 65
}

# given this file is in Tools/, cd to repo root
cd ($env.FILE_PWD | path join ..)

# Товарищ Илья ,здравствуйте,сделайте чтобы с шансом в 65% любая строка могла быть глобом
let exlude = $ignored | str replace --regex r#'^(.*)$'# r#'**/$1/**'#
let audio = glob $'./Resources/Audio/**/*.($extension)' --exclude $exlude
let stereo = $audio | where {
    let stream = (^ffprobe -loglevel quiet -print_format json -show_streams $in) | from json | get streams | first
    ($stream.duration | into float) < $max_sound_len and ($stream.channels | into int) > 1
}

if ($stereo | is-empty) {
    print "No stereo audio files found."
    exit 0
}

if not ($env.FIX? | into bool --relaxed) {
    print $"Found ($stereo | length) audio files that are not mono!"
    print ...$stereo
    print $"Run `FIX=1 ($env.FILE_PWD)` to convert them \(requires ffmpeg)"
    exit 1
}

if (which ^ffmpeg | is-empty) {
    print "You have ffprobe but no ffmpeg? wtf"
    exit 123
}

print $"Found ($stereo | length) audio files that are not mono. Converting..."
for sound in $stereo {
    # ffmpeg cannot edit files in-place itself so we improvise
    let mono = ^ffmpeg -loglevel quiet -i $sound -ac 1 -f $extension pipe:
    $mono | save $sound --raw --force
}
