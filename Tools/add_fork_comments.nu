#!/usr/bin/env nu

const fork = "inky"
let diff = git diff master... --unified=0 -- "*.yml"

# regex hell
const diff_rx = r#'^diff --git .*$'#
const num_rx = r#'^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@$'# # @@ -65 +65 @@
const line_rx = r#'^\+(.*)$'#
const ignored_rx = [
    r#'^new file mode \d{6}$'#
    r#'^index .{10}\.\..{10}(?: \d{6})?$'# # index goidanabeg..nabeggoida 100644
    r#'^-.*$'# # who cares about removals
    r#'^\\ No newline at end of file$'#
]

# given this file is in Tools/, cd to repo root
cd ($env.FILE_PWD | path join ..)

let files = $diff | lines | split list --regex $diff_rx | where {|list| $list | is-not-empty}

$files | each {|file|
    let filtered = $file | where {|line| # file is already a string list
        $ignored_rx | all { |rx| $line !~ $rx }
    }

    let blocks = $filtered | split list --split before --regex $num_rx

    # now since we're inside a scope of a single file, we can just do this
    let path = $blocks | first | first | str replace "+++ b" "."
    if ($path | str contains --ignore-case $fork) { return }

    $blocks | skip | each {|block|
        # literally same shit
        let line_start = $block | first | str replace --regex $num_rx "$1" | into int

        $block | skip | enumerate | each {|line|
            if ($line.item | str contains --ignore-case $fork) { return }
            let num = $line.index + $line_start
            if ($line.item | str trim) == "+" { return } # don't modify empty lines
            let line = $line.item | str replace --regex $line_rx $'$1 # ($fork)'
            let query = $'($num)c\($line)'
            sed -i $query $path
        }
    }
}

"done"
