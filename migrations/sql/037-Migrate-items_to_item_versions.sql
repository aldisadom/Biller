--liquibase formatted sql

-- changeset Aldis:37
-- comment: Rebuild item versions from invoice lines, then stamp every invoice line with the version it matches.
-- comment: A rebuilt changed_date is the date that name and price were first invoiced, not when the item was edited; the current version's is never earlier than an older version's, and is now() when it was never invoiced. No rebuilt date is later than now() (an invoice can be dated in the future).

-- 1. Versions of the items that still exist: each distinct (name, price) found on their invoice lines,
--    oldest invoice first, then the item's current name and price as its last version. The current version
--    takes its name and price from items, not from a line, so GET /items prints the price as before
--    (numeric 50 and 50.0 are equal, but print differently). When it was invoiced, it is dated by the latest
--    first use of any of its item's pairs, so dates never go back as versions go up (50 in January, 60 in
--    February and 50 again now gives v1 = 60 in February and v2 = 50 dated February too). When it was
--    never invoiced, it is dated now(). Every rebuilt date is capped at now(): created_date is set by the
--    user and can be in the future, and later edits are dated now().
WITH lines AS (
    SELECT (l.value->>'Id')::uuid AS item_id,
           l.value->>'Name' AS name,
           (l.value->>'Price')::numeric AS price,
           inv.created_date
    FROM invoices inv
    CROSS JOIN LATERAL jsonb_array_elements(
        CASE WHEN jsonb_typeof(inv.items_data::jsonb) = 'array' THEN inv.items_data::jsonb ELSE '[]'::jsonb END) l
),
candidates AS (
    SELECT l.item_id, l.name, l.price, MIN(l.created_date) AS first_used, false AS is_current
    FROM lines l
    JOIN items i ON i.id = l.item_id
    WHERE l.name IS NOT NULL AND l.price IS NOT NULL
      AND (l.name, l.price) IS DISTINCT FROM (i.name, i.price)
    GROUP BY l.item_id, l.name, l.price
    UNION ALL
    SELECT i.id, i.name, i.price,
           (SELECT MIN(l.created_date) FROM lines l WHERE l.item_id = i.id AND l.name = i.name AND l.price = i.price),
           true
    FROM items i
)
INSERT INTO item_versions (item_id, version, name, price, changed_date)
SELECT item_id,
       ROW_NUMBER() OVER (PARTITION BY item_id ORDER BY is_current, first_used, name, price),
       name,
       price,
       LEAST(CASE WHEN is_current AND first_used IS NOT NULL THEN MAX(first_used) OVER (PARTITION BY item_id)
                  ELSE first_used END::timestamptz, now())
FROM candidates;

-- 2. Add "Version" to every line that matches a version. A line of a deleted item, or with no name or price,
--    stays as it is. The LEFT JOIN keeps every line, in its order. Invoices without lines aren't touched.
--    items_data is TEXT: jsonb reorders keys and adds spaces, which Newtonsoft reads the same.
UPDATE invoices inv
SET items_data = s.items::text
FROM (
    SELECT inv2.id,
           jsonb_agg(CASE WHEN v.version IS NULL THEN l.value
                          ELSE l.value || jsonb_build_object('Version', v.version) END
                     ORDER BY l.ord) AS items
    FROM invoices inv2
    CROSS JOIN LATERAL jsonb_array_elements(
        CASE WHEN jsonb_typeof(inv2.items_data::jsonb) = 'array' THEN inv2.items_data::jsonb ELSE '[]'::jsonb END)
        WITH ORDINALITY AS l(value, ord)
    LEFT JOIN item_versions v
        ON v.item_id = (l.value->>'Id')::uuid
       AND v.name = l.value->>'Name'
       AND v.price = (l.value->>'Price')::numeric
    GROUP BY inv2.id
) s
WHERE s.id = inv.id;

-- rollback UPDATE invoices inv SET items_data = s.items::text FROM (SELECT inv2.id, jsonb_agg(CASE WHEN jsonb_typeof(l.value) = 'object' THEN l.value - 'Version' ELSE l.value END ORDER BY l.ord) AS items FROM invoices inv2 CROSS JOIN LATERAL jsonb_array_elements(CASE WHEN jsonb_typeof(inv2.items_data::jsonb) = 'array' THEN inv2.items_data::jsonb ELSE '[]'::jsonb END) WITH ORDINALITY AS l(value, ord) GROUP BY inv2.id) s WHERE s.id = inv.id;
-- rollback DELETE FROM item_versions;
