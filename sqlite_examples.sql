PRAGMA foreign_keys = ON;
drop view if exists edge;
drop table if exists vertex;
create table vertex(
    idv integer primary key autoincrement default 0,
    ida integer default null,
    idb integer default null,
    x real default 0,
    y real default 0,
    foreign key (ida) references vertex(idv) on delete set null on update cascade,
    foreign key (idb) references vertex(idv) on delete set null on update cascade
);
create view edge (ida, idb, xa, ya, xb, yb) as
    select min(va.idv, vb.idv) as _ida, max(va.idv, vb.idv) as _idb, va.x, va.y, vb.x, vb.y
    from vertex va, vertex vb
    where vb.ida = va.idv or va.idb = vb.idv
    group by _ida, _idb;



begin transaction;

insert into vertex (x, y) values (114, 514);
insert into vertex (x, y) values (1919, 810);
insert into vertex (x, y) values (810, 893);

update vertex set ida=2 where idv=1;
update vertex set idb=1 where idv=2;
update vertex set ida=1 where idv=3;
update vertex set idb=3 where idv=1;
update vertex set ida=2 where idv=3;
update vertex set ida=3 where idv=2;

commit;


select * from vertex;
select * from edge;