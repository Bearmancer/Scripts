1. Read all documents inside markdown
7. Find how to get raw response by reading suggestion: Raw Response.md
2. Assess current invocations of metabrainz using search
3. Compare with guide
4. Assess for improvements
5. Create migration plan 
6. Implement 

----

Ensure state is in one place -- i.e. repo level not inside csharp + create dumps inside state folder not unnested (is that what it is called?) + explain rationale for internal vs file wherever you've used it + explain if changing all to public would reduce verbosity + forget everything I said previously and this is how the music scraping should look like (both MB and Discogs) -- also I deleted JSONs/raw dumps:

1. Start scraping any release -- box set naming is not correct as it could be any release
2. Search all instances of box or boxset or box set and rename to accurately reflect the scope not being restricted to merely box sets
3. Keep raw dumps of all API calls inside state/dump
4. I presume parsing of works happens based on what is returned from Metabrainz? Therefore reconstructing should be very easy, no? I ask because earlier you said that only a rough approximation is possible.
5. Structure all API calls inside dump with appropriate naming schemes
6. Create log file (not inside ./csharp) to show info of all data parsed so far (higher level overview)
7. Allow being able to resume if the operation was abruptly cancelled
8. Check when starting if any earlier data exists -- or start afresh -- print to terminal only if pre-existing data was found
9. Assess whether log is better or the dump for fastest way to resume operation
10. Printing progress bar (the way YT orchestrator has it -- explain why both look different)
11. Show top 5 rolling track details
12. Update table in real time -- show columns and data reflecting either of the two services -- for now MusicBrainz being prioritized
13. After each work has been parsed, i.e. a new work is added indicating the previous work has finished, write it to CSV
14. Find best location to write this file to
15. After all works have been parsed, i.e. the final work has finished, push to a new Google Sheet with the name of the box set (in fact call the CSV that too)
16. Edit editorconfig to show collectionexpression and unnecessary full name of method calls as warnings instead of mere suggestions (red vs yellow)
17. How does one force that when running dotnet build?
18. Test folder has been purged
19. Why does csharp have logs?
20. Check all csharp functions to ensure that there is no duplication of data being nested inside csharp when it should be at repo level
21. Create a method that autofills missing fields from my sheets of rankings of classical recordings searching first MusicBrainz and then Discogs if MB comes up empty -- define scheme of search --- how each query would be structured -- refer to layout and also the actual missing fields in missing fields.tsv (tsv? just copied from Sheets) -- of course `empty` in and of itself is a matter of solo vs concerto vs orchestral recording
22. How to force Sheets to allow tables to be sortable regardless of it being a "table" of Sheets or not - 