"""T7: Architecture Consolidation - Verify type system and module structure."""

import sys
from pathlib import Path
from typing import get_args, get_origin

from toolkit import audio
from toolkit.types import AudioTier


class TestAudioTierTypes:
    """T7.2: Verify AudioTier TypedDict usage across modules."""

    def test_audio_tier_is_typed_dict(self) -> None:
        """Verify AudioTier has sample_rate and bit_depth keys."""
        tier: AudioTier = {"sample_rate": 44100, "bit_depth": 16}
        assert tier["sample_rate"] == 44100
        assert tier["bit_depth"] == 16
        assert set(tier.keys()) == {"sample_rate", "bit_depth"}

    def test_flac_44_uses_audio_tier(self) -> None:
        """Verify FLAC_44 entries are AudioTier instances."""
        assert len(audio.FLAC_44) == 3
        for tier in audio.FLAC_44:
            assert "sample_rate" in tier, f"Missing sample_rate in {tier}"
            assert "bit_depth" in tier, f"Missing bit_depth in {tier}"
            assert isinstance(tier["sample_rate"], int)
            assert isinstance(tier["bit_depth"], int)

    def test_flac_48_uses_audio_tier(self) -> None:
        """Verify FLAC_48 entries are AudioTier instances."""
        assert len(audio.FLAC_48) == 3
        for tier in audio.FLAC_48:
            assert "sample_rate" in tier, f"Missing sample_rate in {tier}"
            assert "bit_depth" in tier, f"Missing bit_depth in {tier}"
            assert isinstance(tier["sample_rate"], int)
            assert isinstance(tier["bit_depth"], int)

    def test_get_flac_tiers_returns_audio_tier(self) -> None:
        """Call get_flac_tiers with known input, verify return type."""
        result = audio.get_flac_tiers(176400, 24, "all")
        assert result is not None
        assert len(result) > 0
        for tier in result:
            assert "sample_rate" in tier
            assert "bit_depth" in tier
            assert isinstance(tier["sample_rate"], int)
            assert isinstance(tier["bit_depth"], int)


class TestModuleStructure:
    """T7.1 & T7.3: Verify centralized utilities and no circular imports."""

    def test_run_command_only_in_utils(self) -> None:
        """Verify run_command is defined only in utils.py."""
        toolkit_dir = Path(__file__).parent.parent / "toolkit"
        occurrences = 0
        correct_location = False

        for py_file in toolkit_dir.rglob("*.py"):
            if py_file.name.startswith("__"):
                continue
            content = py_file.read_text(encoding="utf-8")
            if "def run_command" in content:
                occurrences += 1
                if py_file.name == "utils.py":
                    correct_location = True

        assert occurrences == 1, f"run_command defined in {occurrences} locations (expected 1)"
        assert correct_location, "run_command not found in utils.py"

    def test_no_circular_imports(self) -> None:
        """Import all modules in sequence, verify no ImportError."""
        modules_to_clear = [m for m in sys.modules if m.startswith("toolkit.")]
        for module in modules_to_clear:
            del sys.modules[module]

        import toolkit.types
        import toolkit.exceptions
        import toolkit.utils
        import toolkit.logging_config
        import toolkit.cuesheet
        import toolkit.filesystem
        import toolkit.audio
        import toolkit.cli

        assert hasattr(toolkit.types, "AudioTier")
        assert hasattr(toolkit.types, "AudioFormat")
        assert hasattr(toolkit.utils, "run_command")
        assert hasattr(toolkit.exceptions, "ToolkitError")
        assert hasattr(toolkit.logging_config, "get_logger")
        assert hasattr(toolkit.cuesheet, "process_cue_file")
        assert hasattr(toolkit.filesystem, "make_torrents")
        assert hasattr(toolkit.audio, "get_flac_tiers")
        assert hasattr(toolkit.cli, "main")


class TestAudioFormatUsage:
    """T7.2: Verify AudioFormat Literal is used consistently."""

    def test_audio_module_uses_audio_format_type(self) -> None:
        """Verify audio.py functions accept AudioFormat parameter."""
        import inspect
        from typing import Literal

        sig = inspect.signature(audio.get_flac_tiers)
        fmt_param = sig.parameters.get("fmt")
        assert fmt_param is not None
        annotation = fmt_param.annotation
        assert get_origin(annotation) is Literal
        assert set(get_args(annotation)) == {"16bit", "cd", "all", "24-bit", "mp3"}

        sig = inspect.signature(audio.process_sacd_directory)
        fmt_param = sig.parameters.get("fmt")
        assert fmt_param is not None
        annotation = fmt_param.annotation
        assert get_origin(annotation) is Literal
        assert set(get_args(annotation)) == {"16bit", "cd", "all", "24-bit", "mp3"}
