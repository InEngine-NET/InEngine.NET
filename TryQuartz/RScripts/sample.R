library(RJSONIO)
#library('datasets')
#cat(toJSON(cars))

dtf <- structure(
	list(word = structure(1:2, .Label = c("John", "Jane"), class = "factor"),
       frequency = c(32, 128)), 
  .Names = c("Name", "Age"), 
  row.names = c(NA, -2L), 
  class = "data.frame"
)

#cat(toJSON(as.data.frame(t(dtf))))
#cat(toJSON(data.frame(t(dtf))))

# Taken from: http://theweiluo.wordpress.com/2011/09/30/r-to-json-for-d3-js-and-protovis/
toJSONArray <- function(dtf){
  clnms <- colnames(dtf)
  name.value <- function(i){
    quote <- '';
    if(!class(dtf[, i]) %in% c('numeric', 'integer')){
      quote <- '"';
    }
    paste('"', i, '" : ', quote, dtf[,i], quote, sep='')
  }
  objs <- apply(sapply(clnms, name.value), 1, function(x){paste(x, collapse=', ')})
  objs <- paste('{', objs, '}')
  res <- paste('[', paste(objs, collapse=', '), ']')
  return(res)
}

cat(toJSONArray(dtf))
