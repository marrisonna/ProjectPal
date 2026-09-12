-- TEMPORARY testing aid, not part of the real example/imported data above
-- (001_example_data.sql, 002_team2_from_v1.sql) -- those two files are left
-- completely untouched by this one. This script runs *after* both have
-- loaded (numbered 999 so scripts/setup.ps1's own "every *.sql in this
-- folder, sorted by name" loop always applies it last) and nudges forward
-- any Project date that's old enough to be unhelpful for whatever testing
-- is currently underway, without altering the underlying seed data files
-- themselves.
--
-- Rule: any project.start_date or project.due_date earlier than 1-Dec-2014
-- is pushed forward by exactly 4372 days. Requested as a standing step of
-- every database rebuild from here on -- keep this file in place and let
-- setup.ps1 keep running it automatically until told to stop, at which
-- point simply delete this file (or ask Claude to).

SET search_path TO projectpal;

BEGIN;

UPDATE project
SET start_date = start_date + 4372
WHERE start_date < DATE '2014-12-01';

UPDATE project
SET due_date = due_date + 4372
WHERE due_date < DATE '2014-12-01';

COMMIT;
