const BASE_DIR = "C:\\Users\\Lance\\Desktop\\whisper_tests"

const MODELS = [
    "distil-small.en"
    "distil-medium.en"
    "distil-large-v3.5"
]

const BATCH_MODES = [
    { name: "unbatched",  batch_size: null }
    { name: "batched_4",  batch_size: 4 }
    { name: "batched_8",  batch_size: 8 }
    { name: "batched_12", batch_size: 12 }
    { name: "batched_16", batch_size: 16 }
]

def cleanup_temp_folders [] {
    print "INFO: Cleaning up temporary folders..."
    let protected = ["results", "output"]
    
    ls $BASE_DIR
    | where type == dir
    | where ($it.name | path basename | $in not-in $protected)
    | each {|dir|
        print $"INFO: Removing ($dir.name | path basename)"
        rm -rf $dir.name
    }
}

def get_duration [file: path] {
    ^ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $file
    | complete
    | get stdout
    | str trim
    | into float
}

def main [] {
    let files = (
        ls $BASE_DIR
        | where type == file
        | where ($it.name | path parse | get extension) in [mp4 mkv avi mov webm]
    )

    print $"INFO: Found ($files | length) video files"

    cleanup_temp_folders

    $files | each {|file|
        let input_path = ($file.name | path expand)
        let filename = ($input_path | path basename)
        let file_duration = get_duration $input_path

        print $"\n═══════════════════════════════════════════════════════════════"
        print $"File: ($filename)"
        print $"Duration: ($file_duration | math round -p 2)s"
        print $"═══════════════════════════════════════════════════════════════\n"

        let results = (
            $MODELS | each {|model|
                $BATCH_MODES | each {|mode|
                    let output_path = (
                        [$BASE_DIR, "output", $"($model) - ($mode.name)"]
                        | path join
                    )

                    mkdir $output_path

                    print $"INFO: Running ($model) with ($mode.name) in ($output_path)..."

                    let start = date now

                    let base_args = [
                        $input_path
                        --model $model
                        --language en
                        --output_dir $output_path
                        --verbose True
                    ]

                    let batch_args = if ($mode.batch_size != null) {
                        [--batched --batch_size $mode.batch_size]
                    } else {
                        []
                    }

                    let args = [...$base_args ...$batch_args]

                    ^whisper-ctranslate2 ...$args | complete

                    let elapsed = (date now) - $start
                    let seconds = ($elapsed / 1sec)

                    {
                        Model: $model
                        Mode: $mode.name
                        Seconds: $seconds
                        Duration: ($elapsed | format duration sec)
                        TimePerSec: ($seconds / $file_duration)
                    }
                }
            } | flatten
        )

        let sorted = ($results | sort-by Seconds)
        let baseline = ($sorted | first | get Seconds)

        print "\n═══ RESULTS ═══\n"

        $sorted
        | insert Multiplier {|row| ($row.Seconds / $baseline) | math round -p 2 }
        | insert FileDuration ($file_duration | math round -p 2)
        | select Model Mode Seconds TimePerSec Multiplier
        | table -e

        cleanup_temp_folders
    }

    print "\nINFO: Benchmark complete"
}